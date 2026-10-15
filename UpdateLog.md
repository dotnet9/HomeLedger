# 更新日志

## 0.1.7 (2026-10-15)

- 使用 Semi、Ursa 与 CodeWF 共享主题，支持三种模式、八套配色与偏好持久化。
- 简体中文与 English 共用 Toolkit I18nManager 单例；T4 生成语义键，业务 JSON 随应用发布至 I18n，兼容 macOS bundle 的 Resources 路径。
- 新增完整关于窗口，接入 Toolkit 的检查、下载、校验、安装、取消、重试与缺少资产回退。
- 补齐账目分页、存款选择及结算、家庭成员收支比较；修复日期输入重叠、分页不可见、环图大弧与图例语言问题。
- 数据库访问使用 Dapper.AOT 源生成，保留原有数据库结构、密码算法与事务规则，解决 NativeAOT 启动失败。
- 提供可单独复制的离线交互原型 design/index.html，以及无桌面 UI 验证和原生运行验证入口。
- 五个原生 runner 分别构建 win-x64、linux-x64、linux-arm64、osx-x64、osx-arm64，只发布 setup.exe/deb/dmg 及 SHA-256。
