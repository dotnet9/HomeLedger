# 无桌面验证

验证器启动真实 App 和 Avalonia Headless + Skia，在独立目录创建 SQLite fixture。不会打开普通桌面应用、浏览器或真实安装器。更新服务、浏览器与退出动作均注入替身。

```powershell
dotnet test tests/HomeLedger.Core.Tests/HomeLedger.Core.Tests.csproj -c Release
dotnet run --project scripts/ui-verification/UiVerification.csproj -c Release -- E:/github/temp/homeledger/managed
dotnet publish scripts/ui-verification/UiVerification.csproj -c Release -r win-x64 -p:PublishAot=true -p:IlcGenerateCompleteTypeMetadata=true -p:IlcTrimMetadata=false -p:IlcSingleThreaded=true -p:CompressSymbols=false -o E:/github/temp/homeledger/native
E:/github/temp/homeledger/native/UiVerification.exe E:/github/temp/homeledger/native-run
node scripts/build-prototype.cjs
node scripts/prototype-verification.cjs E:/github/temp/homeledger/prototype
```

原型验证需要可解析的 Playwright 与已安装的 Chromium，只使用 headless 模式。首先将 design/index.html 复制到仅包含该文件的目录，再验证十页导航、语言/主题保留、更新取消、记录增删改和所选存款结算，检查外部请求及脚本异常。

UI 验证器输出 assertions.txt 和 PNG：七个业务页面的双语截图、登录、关于、外观，以及八套配色的浅色/深色快照。185 项断言覆盖真实记账、过滤、软删除、分页、数据隔离、所选存款结算及支取、成员搜索、偏好、版本、作者、图表语言与更新状态；新增真实 Ursa 三态按钮点击、Studio 配色保留、八色直接选择、语言列表及模式提示动态切语、980/1280 标题栏边界。

发布程序另有显式 `--verify-runtime <独立临时目录>` 参数，只验证原生代码与 I18n、登录、金额/日期、筛选、结算和支取。运行成功写入 runtime-verification.txt；托管程序拒绝该参数。五个平台工作流均在打包前执行此入口。

全部测试均不能替代真实系统安装体验；当前验证不执行安装器，不测试 macOS 公证与 Linux 桌面环境。

真实关于窗口增加动态 CanMinimize / CanMaximize 能力、按钮禁用、程序化点击守卫与原状态恢复断言。
