# HomeLedger 家庭记账工具 — 设计文档

> 版本：v0.1（2026-08-21）
> 前置文档：[HomeLedger家庭记账工具需求.md](./HomeLedger家庭记账工具需求.md)

## 1. 总体架构

沿用 Zitie 项目骨架，三个项目 + 统一包版本管理：

```
HomeLedger/
├── Directory.Build.props        # 共享编译属性
├── Directory.Packages.props     # 中央包版本管理（CPM）
├── HomeLedger.slnx
├── docs/                        # 文档
└── src/
    ├── HomeLedger.Core/         # 纯逻辑：模型、数据访问、利息计算、报表导出（可单测）
    ├── HomeLedger.Avalonia/     # UI：视图、ViewModel、主题、图表
    └── HomeLedger.Desktop/      # 程序入口、DI 组装、发布配置
```

依赖方向：`Desktop → Avalonia → Core`，Core 不依赖任何 UI 库。

### 技术栈

| 项 | 选型 | 说明 |
|----|------|------|
| 运行时 | .NET 10 | 与 Zitie 一致 |
| UI | Avalonia 12 + Semi.Avalonia | 中文友好、明暗主题 |
| MVVM | Prism.Avalonia / Prism.DryIoc.Avalonia | 与 Zitie 一致，`BindableBase` + `DelegateCommand` + Prism 应用入口 |
| 数据库 | SQLite + Microsoft.Data.Sqlite + Dapper | 单文件、零运维 |
| 图表 | LiveCharts2 (LiveChartsCore.SkiaSharpView.Avalonia) | UI 展示与 PNG 导出复用 |
| PDF | QuestPDF | 图文报表排版（非商用免费） |
| Word | DocumentFormat.OpenXml | 生成 .docx |
| 密码 | PBKDF2（Rfc2898DeriveBytes） | 内置于 Core，无额外依赖 |

### 数据库位置

`%LOCALAPPDATA%\HomeLedger\ledger.db`；首次启动自动建库、建表、初始化管理员。

## 2. 数据模型

### 2.1 ER 概览

```
Users 1──* Transactions
Users 1──* Deposits
Users 1──* Categories（个人自定义分类）
Deposits 1──0..1 Deposits（结转来源，SourceDepositId）
Users 1──* InterestSettlements
```

### 2.2 表结构

**Users**

| 列 | 类型 | 说明 |
|----|------|------|
| Id | INTEGER PK | |
| Username | TEXT UNIQUE | 登录名 |
| DisplayName | TEXT | 显示名（爸爸/妈妈/儿女…） |
| PasswordHash | TEXT | PBKDF2，格式 `iterations.salt.hash` Base64 |
| Role | TEXT | `Admin` / `Member` |
| IsActive | INTEGER | 停用后不能登录，数据保留 |
| MustChangePassword | INTEGER | 首次登录/重置后强制改密 |
| CreatedAt | TEXT | ISO8601 |

**Categories**

| 列 | 类型 | 说明 |
|----|------|------|
| Id | INTEGER PK | |
| UserId | INTEGER NULL | NULL=内置分类；否则为个人自定义 |
| Name | TEXT | 分类名 |
| Kind | TEXT | `Income` / `Expense` |
| SortOrder | INTEGER | |

内置分类随建库种子写入（UserId=NULL，只读）。

**Transactions（收支记录）**

| 列 | 类型 | 说明 |
|----|------|------|
| Id | INTEGER PK | |
| UserId | INTEGER FK | 归属成员 |
| Kind | TEXT | `Income` / `Expense` |
| Amount | TEXT | decimal 字符串存储，避免浮点 |
| Date | TEXT | 记账日期（yyyy-MM-dd） |
| CategoryId | INTEGER FK NULL | |
| Note | TEXT | 备注 |
| IsDeleted | INTEGER | 逻辑删除 |
| CreatedAt / UpdatedAt | TEXT | |

**Deposits（家庭存款/利息记录）**

| 列 | 类型 | 说明 |
|----|------|------|
| Id | INTEGER PK | |
| UserId | INTEGER FK | 存款人 |
| Kind | TEXT | `Deposit`（存入）/ `Withdrawal`（支取）/ `Interest`（利息结转） |
| Amount | TEXT | decimal 字符串 |
| Date | TEXT | 起息日/发生日 |
| AnnualRate | TEXT | 年利率（如 0.10）；`Interest` 类型为其来源利率快照 |
| SourceDepositId | INTEGER NULL | 结转来源（Interest → 原 Deposit/上一次 Interest） |
| SettlementId | INTEGER NULL | 关联的结算批次 |
| Note | TEXT | |

利息链：一笔存款结转后形成链表 `Deposit → Interest → Interest → …`，链上任意时刻**只有末端未支取的记录继续起息**，起息日为链末端记录的 Date。

**InterestSettlements（结算历史）**

| 列 | 类型 | 说明 |
|----|------|------|
| Id | INTEGER PK | |
| UserId | INTEGER FK | |
| SettledAt | TEXT | 结算日 |
| DepositCount | INTEGER | 涉及笔数 |
| TotalInterest | TEXT | 利息总额（decimal 字符串） |
| CreatedBy | INTEGER FK | 操作人（成员自己或管理员代操作） |

金额一律 `TEXT` 存储 decimal（SQLite 无原生 decimal），读写经 Dapper type handler 转换。

### 2.3 事务与删除

- 结算 = 单个 SQLite 事务：写 N 条 Interest 存款 + 1 条 Settlements，失败整体回滚。
- 收支/存款记录逻辑删除（IsDeleted）；存款链上的记录不允许删除中段（只能支取整链或删除末端未结转记录）。

## 3. 利息计算（核心域逻辑）

```
应计利息(单笔, 截至日 settleDate)
  = 本金 × 年利率 × max(0, (settleDate - 起息日).Days) / 365
  → MidpointRounding 保留 2 位
```

- 单利按日计息；复利通过"结算结转"实现：结算生成新 Interest 记录（本金=利息额，起息日=结算日，利率沿用来源利率），原链停息。
- 支取：对某条存款链发起支取，金额可部分支取（部分支取拆分为：原链按支取日停息结清支取部分 + 剩余本金生成新存款记录继续起息，利率不变）。
- 结算范围：默认对该用户全部"未结清"存款链结算，界面上可勾选部分。
- 纯函数实现于 `InterestCalculator`，输入（本金、年利率、起息日、截至日），输出利息与本息合计——Core 项目单测覆盖重点（闰年按 365 固定、跨年、当日存当日结、部分支取拆分）。

## 4. 权限与安全

- 登录成功后 `SessionContext` 保存当前用户（Id、Role、DisplayName），随 DI 作用域传递。
- 数据访问层（Repository）所有查询强制带 `UserId` 过滤：Member 传入 Session 的 UserId；Admin 查看传目标 UserId 或全部——UI 层不拼权限，杜绝越权。
- 管理员无记账入口（界面不出现记账页签，Repository 也拒绝为 Admin 写收支记录）。
- PBKDF2：100_000 次迭代 + 16 字节盐 + SHA256，恒定时间比较。

## 5. UI 设计

### 5.1 页面结构

```
LoginWindow
└── MainWindow
    ├── Member 视角（成员）
    │   ├── 记账页：快速记账表单 + 近期记录列表（筛选/编辑/删除）
    │   ├── 存款页：存款链列表（本金/利率/起息日/应计利息/本息合计）、存入/支取/结算
    │   ├── 报表页：自己的汇总图表 + 导出
    │   └── 设置页：修改密码
    └── Admin 视角（管理员）
        ├── 成员管理：账号增删启停、重置密码
        ├── 全家总览：成员对比、月度趋势、分类占比（图表）
        ├── 明细查询：按成员/日期/类型筛选全部明细
        └── 报表导出：全家报表（含存款利息）
```

### 5.2 关键交互

- 快速记账：金额大输入框 + 类型切换 + 分类下拉 + 日期（默认今天），回车保存。
- 存款页每行实时显示应计利息；"结算"按钮弹出确认框，展示将被结转的笔数与总额。
- 报表导出：选择时间范围 → 选择格式（CSV/PNG/PDF/DOCX）→ SaveFileDialog。

## 6. 报表导出设计

统一入口 `IReportExporter`（Core），按格式实现：

| 格式 | 实现 | 内容 |
|------|------|------|
| CSV | 手写（UTF-8 BOM，Excel 直接打开不乱码） | 明细 + 汇总表 |
| PNG | LiveCharts2 离屏渲染图表（SKBitmap → PNG） | 汇总图表拼图 |
| PDF | QuestPDF | 封面、汇总表、图表位图、明细表、存款利息表 |
| DOCX | OpenXML SDK | 同 PDF 结构，图表以 PNG 嵌入，表格用 Table 网格 |

报表数据由 `ReportBuilder` 一次性组装为中间模型（`ReportModel`：汇总行、分类统计、月度序列、成员对比、存款利息明细），四种导出器只做渲染，逻辑不重复。

## 7. 项目内部分层

```
HomeLedger.Core/
├── Models/            # POCO：User, Transaction, Deposit, Category, Settlement...
├── Data/              # Db migrator（建库建表种子）、Repository、TypeHandlers
├── Services/
│   ├── AuthService    # 登录、改密、PBKDF2
│   ├── LedgerService  # 收支 CRUD、汇总查询
│   ├── DepositService # 存入/支取/结算（事务）
│   ├── InterestCalculator # 纯函数利息计算
│   └── ReportBuilder  # ReportModel 组装
└── Export/            # CsvExporter, PdfExporter, DocxExporter, PngChartExporter(接口注入)

HomeLedger.Avalonia/
├── Views/ ViewModels/ # 页面
├── Controls/          # 图表卡片、金额输入等复用控件
└── Converters/

HomeLedger.Desktop/
├── Program.cs / App.axaml
└── CompositionRoot   # DI 注册（SQLite 连接、服务、Session、页面路由）
```

## 8. 测试策略

- `tests/HomeLedger.Core.Tests`：利息计算（单利、结转复利、部分支取、边界日期）、Repository 数据隔离（Member 看不到他人数据）、结算事务原子性、报表数据组装。
- UI 手工验收清单随 README 提供。

## 9. 里程碑

| 阶段 | 内容 |
|------|------|
| M1 | 项目骨架 + 建库 + 登录/账号管理 |
| M2 | 成员记账（CRUD/筛选/汇总） |
| M3 | 存款与利息（含结算、部分支取） |
| M4 | 管理员总览（图表）+ 报表导出四格式 |
| M5 | 备份、打磨、发布（publish 脚本仿 Zitie） |
