# Demo 画面导出与视觉核验

description：在首个Demo已提交并推送后，按用户持续优化授权增加游戏自身画面导出，补齐实际渲染证据。

基线：`08fad628a404202536aab05bf222ad54529140b9` 已推送到 GitHub main，本地/远端 SHA 一致。工作分支 `feature/demo-diagnostics`。这是已有诊断Demo的有限改进，沿用单人测试、规则与UI分离及最终画风未定的边界。

新增 `DemoCapture`：默认启动不写文件；F12或显式 `--capture-demo` 才导出本游戏帧缓冲中的图像。输出仅在应用自己的 `DemoCaptures` 下，用GUID文件名与CreateNew防覆盖；编码先于文件创建，写失败只清理本次创建的文件。纹理和文件流均释放。CLI成功退出0、失败退出1；普通F12不退出游戏。

实际验证：

- 旧播放器不识别新参数，5秒内不退出，也没有保存标记：`capture-red.log`。仅清理该次测试进程。
- 新版本隐藏窗口没有可导出帧，明确报错并退出1：`capture-green.log`（文件名不表示通过）。
- 新版本正常显示窗口后，成功保存PNG并退出0：`capture-visible.log`。人工查看20261013-085809导出图，中文字体、国家列表、OPS和手牌区均正常；图片保留为 `Docs/Images/first-demo.png`。
- `capture-regression.xml`：52/52真实Unity EditMode回归通过；本改进不新增镜像测试或虚报鼠标验收。
- 独立reviewer未发现阻塞问题，提出失败写入可能留下部分PNG的P3建议；已调整编码顺序，并在写失败时清理本次新文件。

computer-use多次无法激活窗口，捕获的是非游戏画面。窗口实际渲染由游戏内部导出确认，鼠标操作验收仍未完成；两项分别记录。没有尝试用失效坐标操作其他窗口，也没有把导出器扩展为桌面截图工具。

Unity参考：[CaptureScreenshotAsTexture](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/ScreenCapture.CaptureScreenshotAsTexture.html) 要求完成帧渲染后捕获；实现等待 `WaitForEndOfFrame`，实际6000.6播放器验证成功。

下一项：按既有首批事件设计接入可复用影响力效果/选择；正式牌库、出牌生命周期和完整回合继续分阶段实施。

P3改动后再次验证：capture-final.xml为52/52、build-capture-final.log构建成功；capture-final-player.log保存图片并退出0。首个Demo的Git基线已同步；本批继续正常提交与远端核验。
