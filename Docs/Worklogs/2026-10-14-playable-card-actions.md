# SDD ledger — plan: Docs/Plans/2026-10-14-playable-card-actions.md

BASE: 1f4fe1639d4cd395f3e7cf71c6b08759d2f2f2fa

Ruling: 沿用本项目已有的功能分支工作方式，不用指向无关聊天目录的 app worktree 工具。单一实现者，最终独立只读审查；成本是目录不隔离，需保持 owned changes。
Ruling: 用户授权持续开发与 Git 同步，沿用已批准架构。此批不再重复询问实现许可。
Ruling: 先接真实出牌与太空/影响力，再接政变。无完整头条时只能显式称为行动试玩；代价是暂时无法完成标准完整对局。
Pre-flight: T1 immutable GameState -> T2 filtered session -> T3 controller/map; 新 GameState 属性必须在 Apply/With* 复制中保存，投影不可暴露 SpaceAttemptReceipt.Random。

Steam 对照：2026-10-14 阅读官方 https://store.steampowered.com/app/406290/Twilight_Struggle/ ，确认其世界地图/双阵营/单机与双人定位；未运行 Steam 版，不声称实际游玩或细节验证，不复制其美术。

Task 1: in progress

T1 RED: 1574 total / 13 NotImplemented failures, baseline1561 green; core-green1574passed. T1 implemented; T2 in progress.

T2 first test attempt did not compile: test referenced DiscardPileCount instead of actual DiscardCount; corrected fixture assertion, not a behavioral RED.

Correction: compiler evidence actually first stopped at generated escaped quote syntax in JSON privacy test. Both red/red2 are compile failures, not RED. Escaping now corrected; public count typo was also corrected before compile completed.

T2 GREEN1584; T3 RED1589 total/5 NotImplemented failures, GREEN1589. UI first compile failed because generated instruction text contained literal newlines inside C# string; escaped those newlines, retry below.

Boundary tests1599/1598: scoring test assumed seeded USSR hand contained scoring; actual deal does not. Changed only test fixture to move a real scoring card into that hand while conserving the complete card partition; production unchanged.

Task 1: complete — 纯状态/规则与保留复制，行为RED→GREEN及单回合/溢出边界最终1599/1599。
Task 2: complete — 身份/收据/原子状态/隐私，10个会话RED→GREEN。
Task 3: complete — 数据与控制器5个RED→GREEN、界面、28载荷/构建成功、手牌实际渲染。
窗口QA：sky正常返回新版窗口，但首次画面是壁纸且activate失败；按技能重新定位后再次失败，停止输入。游戏自身隐藏导出exit1无帧，可见预览导出exit0并检查1280×820手牌PNG。没有用截图冒充鼠标测试。
Ruling: 延续已有PowerShell Unity测试脚本和工作日志，未运行仅Bash的skill task-done/review-package助手；真实XML/SHA和固定Git差异提供相同证据，不引入一次性工作流脚本。
Task 4: descriptions/Wiki/报告已更新；最终独立审查待运行。

Final review: independent gpt-6-astra high; BASE1f4fe16..e415f13; Critical0/Important0/Minor0, ready to merge. Reviewer independently confirmed final1599 XML and22 changed C# hashes; no additional test runs.
Final: Ruling: 头条/计分/中国牌/对手事件OPS/政变/调整/完整事件/活动效果 — 作为明确拒绝的下一批功能保留，当前只承诺影响力和太空；成本是玩法覆盖有限。
Final: Ruling: 回合末/下一回合/完整胜负/太空额外行动 — 单回合从初始轨道练习，在普通名额边界停止；成本是不能完成标准一局。
Final: Ruling: 任意路线都走满12行动 — 当前不保证；若用本方/中立牌太空，余牌可能全为未接对手事件。Wiki/README明确可能需要重置，后续补事件；成本是试玩路线受限，不能宣称完整可玩回合。
Final: Ruling: 存档/网络/不一致外部GameState构造 — 没有这些运行入口，可信Bootstrap受范围约束；成本是将来接入前必须新增守卫和测试。
Final: Ruling: 真实鼠标验收 — 未通过，工具激活失败；控制器和渲染检查不替代输入验证，继续等待用户前台配合；成本是按钮端到端仍可能存在未发现问题。
Ruling: finishing-a-development-branch 的合并选择使用用户持久授权：主分支常规同步已经明确，不重复展示确认菜单；ff-only，禁止覆盖远程历史。
Task 4: complete — metadata116/116,70datasets,193symbols,586references通过；最终审查通过；常规同步结果见Git提交与远程核对。
