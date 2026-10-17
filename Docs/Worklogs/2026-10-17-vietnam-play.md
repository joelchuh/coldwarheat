# Vietnam playable integration worklog

description: Real event transaction, all map OPS consumers, public receipts and Demo evidence.
Base1137f63a57dce3164171698c4f57c22071e3cc5c verified on GitHub foundation branch. Existing authorizations retained; brainstorming/design and inline executing-plans with TDD applied.
Ruling: Keep flat constructor closed; WithVietnam explicit opt-in — preserve legacy assembly and exclude accidental activation — consumers must use new registration intentionally.
Pre-flight: grant Flow produces temporary resolved execution and public receipt; parent needs cleared execution plus immutable activation in same candidate. RegionalBudget currently China-only; opponent/Olympic paths need same factory. UI activated-effect branch must include grant and conditional label. No FiveYearPlan expansion because Vietnam affiliation is USSR.
Task1 started: tests before source; compile-only registration stub throws. No runtime source changed during prior review. Existing scratch scripts unavailable in PowerShell; equivalent persistent plan/ledger and native command results used, no new reusable script.

Task1 tests staged: direct两方、两种对手顺序、普通/对手/Olympic三种地图OPS、China独立奖励、头条、Host重发、Space/threshold隔离、加点和最终提交溢出。完整RED运行时不修改C#或载荷。后续补合并保底、真实accepted-prefix与回合清理覆盖。

RED vietnam-play-red：3817/3792通过/25失败，全部新fixture注册stub NotImplemented；XML547f6245f1184567f9f644edb085628440a971e6fbcbaef2ac06c0c62fc9dcd1。随后实现显式注册、共享私有grant+activation候选与全地图OPS路径。

首次Core GREEN3817/3815/2失败，仅Olympic模式旧门禁拒绝RegionalBudget。定位WithOperations显式地区拒绝以及GameState原引用拒绝；移除首个门禁后增加实际连续spend/伪造上下文/地区奖励失去测试，定向RED2/0/2均旧GameState拒绝变更。随后仅允许同id/Actor/mode/cost/reachability、同quote/map、spent一致且奖励集合只减的可信地区推进；源Matches仍要求精确原引用和版本/序号/国家/RNG绑定。旧无地区上下文保持相同对象。补合并保底、注册错误与真实回合清理测试。定向流程是第二个独立上下文，尚未满足>=3脚本化门槛；未另造脚本。

Task1: complete; vietnam-play-core-green-2 →3822/3822、0跳过/不确定、Unity0、195828ms；XML34901b613f9d65036e2fdf7628b2a87305b405f37945657ba4cbfc701310e5f2。30项新Core案例。Task2将继续UI与Demo，最终整批审查未进行。

Task1 commit01ef350；元数据185描述/88数据/485符号/750依赖/431文件通过，diff检查通过。Task2先写13项UI/目录测试与编译stub，再跑完整RED；人工fixture新文件此时不进入默认Bootstrap或Player清单。

Demo预检：最终计分源场景的中国牌是美国背面，不能直接继承给越南练习；新fixture将显式保存china_owner/face并严格读取，设苏联正面用于组合OPS体验，不改变正式开局。地区名称新增稳定ID检索键，具体中英文仍由现有本地化数据读取，避免在规则中写显示文案。

Task2 RED3835/3822/13失败，全为新UI/目录测试；10个注册stub失败，3个缺Region/混合头条/政变反馈。随后接实际目录和双Bootstrap/Build，公共条件/即时收据，新增第39人工练习和第71载荷。

Task2原生GREEN3835/3835，0跳过/不确定，185802ms，Unity0，XMLf64fe11a9b5d786a70db3200615788aa0c58e46f2d578080239707059cb8f374。43项新案例。参数文件仅description纠正旧设计状态，未改变规则字段/数值；随后Build预处理将重新严格装载当前全部载荷。独立审查/Player验收待进行。

整批审查（base1137f63..source26fe56a；独立只读一次）：0 Critical /1 Important /0 Minor。Important：对手/Olympic待选用途面板仅显示普通OPS，未展示目标政变报价；中国牌预览仍只计亚洲奖励，显示5而实际东南亚6。父代理逐处核对确实存在；保留普通OPS语义，新增共享Core工厂的公开地区预览，两处确认面板使用目标报价。先加5项预览→实际政变一致性测试，完整RED后修复，一次修复回归，不再复审。

审查未判断项的工程裁定（本批穷尽记录）：
- Player构建/载荷/点击/裁切：父代理单独验收，不以单元测试替代；成本是重建及有限真实点击。
- 远端同步：收尾提交后push并核对远端SHA；成本是网络同步检查。
- Five Year Plan触发越南：当前子事件门禁只支持美国所属牌，越南不符合；保持门禁，成本是此来源不可用。
- 未来未支持事件OPS来源：不扩展至所有卡牌，新增来源需独立规则与来源绑定；成本是后续逐牌实现。
- 同回合不同resolution重复激活：继续RequiresRuling；成本是该边界明确暂停，不自动重复加点。
- 合并最低值：沿用用户已确认PE-INTERPRETATION-01，不重新询问也不宣称官方专门裁定；成本是项目解释需持续标注。
- 文档待验收状态：受审版本描述属实，最终证据到齐后更新；成本是交付文档另一次提交。

首次Player构建26fe56a成功（FIRST_DEMO_BUILD_SUCCEEDED 98134265）；验证命令首次误传绝对player/log路径被INVALID_PATH拒绝，读descriptor后改为仓库相对参数，71载荷/5程序集通过（report SHA6dbd76f74768ad7b77ea86c0132564789fc1e81f238575c881156110b2bea263）。此包随后被预览修复版替换，旧证据不当作新构建证据。
首次真实点击：人工局面39，苏联发动越南，越南0→2、移出1、v3/seq4、换美国；持续条件显示东南亚，展开结果同时显示加点和激活，F12保存应用截图。已安全关闭该Player以修复预览。未把未点击的奥运会/头条路径写成真实UI通过。

审查修复：RED3840/3835/5失败 → GREEN3840/3840、0跳过/不确定、Unity0、180478ms，XMLd75322c1c0dc8622d4d0fcac3aa56230ee877eade62e11438a6c99ccc3098227。保留普通值，条件预览共用Core工厂；两处政变确认读取SelectedCoupQuote。GREEN之后仅调整面板坐标/字号，防止二行警告与放弃按钮裁切；不改变规则或预览逻辑，后续由编译构建与实窗验证。
