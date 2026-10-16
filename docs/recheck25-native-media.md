# 0.1.13 本地候选版本媒体复核

2026-10-16，Windows x64。PNG 从独立 NativeAOT UI 验证器捕获逐字节复制，绘制实际应用控件，使用临时成员、交易和数据库；没有运行安装器或使用真实家庭账本。

本轮生产及验证源码在验证时尚未提交、未打标签，冻结源码指纹为 `f5b1f6b1d55a9b3f7b1fa79457845bb597e740c2749e3aaf4f9e7ef1c3ed6d7a`。指纹规则为按路径排序的 `路径 + NUL + 文件小写 SHA-256 + LF` 记录的 UTF-8 SHA-256；清单保存于 `E:/github/temp/recheck25-homeledger-source-manifest.json`。旧提交、标签和 recheck24 证据不用于替代当前字节验证。

本机验证完成后，生产修复提交为 `2e6d0d83d25f7fb85b3ce35e4f7a1746492126a5`，验证与原型提交为 `97bfdc85144e5ad365f5c3b1c38608778f11e00d`。提交后的工作区冻结文件逐字节复核一致；此指纹不是 Git 树哈希。本文收口时未推送、打标签或公开发布。

| 仓库图片 | NativeAOT 原始捕获 | SHA-256 |
| --- | --- | --- |
| `docs/media/homeledger-v0.1.13-main.png` | `member-0-zh-CN.png` | `94953068eaeb3c990e586c3e746ac17dce6a961f5879f067db0252f5097bf72c` |

正式产品由仓库 `scripts/publish.ps1` 构建，实际 EXE 的隔离 `--verify-runtime` 已通过；独立原生 UI 实际运行通过 648 项断言，输出 `NativeAOT=True`。新增主按钮 160 项状态记录、导航 96 项状态记录，启用主按钮最低对比度 4.8498。托管同样 648/648；Core 26/26；单文件离线原型 81 个页面/场景和 16 套主题快照通过。

实际日志与精确文件哈希收录于 `E:/github/temp/recheck25-homeledger-final-receipt.json`。原安装器脚本及视觉设计保留，仅用逐文件校验的无 PDB/DBG 临时 staging 编译 Windows 安装包，未执行安装器。第三方裁剪/AOT、DAP028 和 CS8602 告警保留在日志中。没有宣称整页 95% 还原、人工安装/升级/卸载、IME、多 DPI、跨屏或其他系统桌面验收完成。
