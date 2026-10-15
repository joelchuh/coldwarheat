# 有限头条事件结算

description: 将已保留/揭示的头条接入既有事件服务，记录RED-GREEN、根责任与终局边界。

基础分支feat/headline-foundation已推送并核对远程278ca1bf9f1a6244993dd5767f01786e2e6806c4；main未变。本批从该点分出feat/headline-event-resolution；计划见同名Docs/Plans。用户询问Unity账号：仅知Hub安装位置，未获许可恢复证据，不猜账号。当前开始Task1。

Task1：18项新游标用例先全部失败于占位，完成后连同79个已有头条用例共97项通过。参数/目录/完整牌区校验、按当前根精确一次、跨回合身份与只读快照均覆盖。未改Practice/Host，尚不能执行头条事件。

Task2：31个行为测试先全部RED，完成后31GREEN且整批1982托管通过。测试首轮因NUnit链式Contain仅接受字符串编译失败，改为集合Superset/Member后才记录RED；实现初版CardId没有运算符重载，按现有IEquatable改用Equals后编译零警告错误。真实功能覆盖即时/七计分/持续OPS、双方出牌方与受益方、等待暂停、终局、后张最新地图、无OPS与普通计数、溢出原子性及第一普通行动。

根上下文存于Practice.Headline，所有同回合构造携带；下一回合唯一入口重置。共享效果入口按已授权卡源调用，Move显式辨别Hand/HeadlineReserved/Resolving；不迁回手牌或伪增CompletedActions。完整Host/选择继续/UI仍未接。

Task3：17个新增选择恢复用例先全部RED，GREEN17，最后全部可用托管1999项通过/0失败/0跳过；新增总66项。五种选择处理器、跨Owner两次等待、旧Decision/错方/错载荷、已发布前缀保持、本次选择后溢出丢草稿、后张读取选点、后张终局、外来executionID、普通事件保持原行为均已验证。CompleteChoice不会把原始效果草稿提前发布；旧快照重放仍由未来Host版本/幂等负责，不把纯函数当宿主。

测试通过的是选定.NET8/NUnit集合，不是Unity EditMode；160项原生测试仍排除，许可证未恢复。整批独立审查与分支推送待完成，现有播放器未变。

独立整批审查针对83236341完成：Critical0/Important0/Minor0；16份源码和8份XML摘要匹配。只批准源检查点，未批准main/Unity/播放器。无须修复；既有1999补充测试证据对应源码未变化，不重复运行。按已有授权保存源分支，远程SHA另外核验。

## 执行账本归档

```text
# SDD ledger — plan: Docs/Plans/2026-10-15-headline-event-resolution.md
Base:278ca1bf9f1a6244993dd5767f01786e2e6806c4; branch feat/headline-event-resolution.
Pre-flight: Task1 immutable order/cursor is consumed by Task2 root-aware state and Next; Task3 choice resumes the same root and then advances. GameState already owns Cards; queue stores references only, never duplicate zones.
Ruling: Same approved E project branch practice and manual briefs/ledger wrapper equivalent (no Bash). Unity gate remains blocked; no main/player changes.
Ruling: Model root purpose using typed HeadlineResolutionState in Practice rather than adding an unused generic event interpreter. Shared effects remain existing pure services. Future nested events will require their own contexts.
Ruling: Task1 only pure cursor; Practice Actor/phase attachment moves to Task2 so tests exercise public GameState transitions without adding test-only friend access.
Task1: in progress.
Task1: complete (278ca1b..ba1ab279060dc662714e90eb8b2390bc419af60e);18 RED failed,97 headline checks GREEN passed,metadata138 passed. Task2 base:ba1ab279060dc662714e90eb8b2390bc419af60e.
Task2: complete (ba1ab27..27b1c2f5eda87ccd3e80cf5e62b3618ae573ab92);31 RED failed,31 GREEN and1982 full managed regression passed;metadata138 passed. Task3 base:27b1c2f5eda87ccd3e80cf5e62b3618ae573ab92.

Task3 source:17 RED failures at stub,17 GREEN, headline-resolution-final.xml1999 passed/0 failed; review pending.
Ruling: CompleteChoice is an authority transaction helper, not a replay/identity boundary; preserve Host integration for next batch and do not expose raw GameState or permit partial publishing.

Task3: complete (27b1c2f..83236341f78d7f20bb84603605a5a04fd7fee1fd);17 RED/17 GREEN,1999 full managed passed; whole-branch review Critical0 Important0 Minor0,16 source and8 XML hashes verified.
Final: Ruling: Host identity/version/replay and player privacy are not provided by these authority drafts — keep inaccessible to current Demo and implement filtered session boundary next — exposing raw transitions would leak state or bypass freshness checks.
Final: Ruling: startup/next-turn/UI activation remains disabled in existing demos — source checkpoint only — enabling without integration would stall or skip phases.
Final: Ruling: Defectors/nested events/event OPS/additional handlers are unsupported and rejected before execution — no silent fallback — full rules remain incomplete.
Final: Ruling: save/replay/network not judged or shipped — no persistence promise — recovery requires a future protocol.
Final: Ruling: managed evidence does not establish Unity/asmdef/native/build/mouse readiness — keep main/player unchanged until license recovery and actual gate — source could still expose native integration faults.
Final review: fresh-context gpt-6-astra approved source checkpoint; no fix pass needed. No deferred minors. Routine feature-branch push authorized by AGENTS22/26; no redundant integration menu.
```
