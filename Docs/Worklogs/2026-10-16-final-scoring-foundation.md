# 最终计分基础工作记录

> description: 六区最终汇总、胜负接缝和正式数据加载的开发、真实测试与边界记录。

基线361b0fc，分支feat/final-scoring-foundation。Task1增加最终定义、只读完整结果和FinalScoringCompletedEvidence；纯Core复用地区服务，不改地图表现。26新增测试覆盖越20后反转、欧洲优先、零分平局、SEA去重、中国牌、错误输入和大数。

原生RED：2346 total /2320 pass /26 fail；首次GREEN编译遇CardId没有!=运算符，改用Equals；随后2345/1，期望漏算古巴邻接美国，经正式地图核对修正测试（生产算法不改）；最终final-scoring-core-verified：2346/2346，0失败/跳过，Unity exit0，XML SHA256 4fd131ebd617dc86b82917d0f112cdc4a93ba2f475e9db5162765b5339659881。

Task2已增加参数schema与正式加载器，34新增测试从RED转GREEN；final-scoring-loader-green全套2380/2380，Unity exit0。最终审查继续；尚未接第10回合宿主/事务/UI，未重建播放器。

## 独立审查与修复门禁

新上下文review_final_scoring_foundation（gpt-6-astra high）审查361b0fc..5ab6c0e：0 Critical、1 Important、0 Minor。发现直接构造通用六地区定义可用未知地区替代欧洲，绕过基础六区语义；正式JSON路径已有约束。接受该问题，增加逐一替换六个地区的真实Core输入测试并在一轮RED/GREEN中修复。审查未覆盖且本批也未宣称完成的部分：最终宿主资格/事务/幂等、UI、动态效果和播放器。

下一批接线草案：[最终计分试玩](../Architecture/final-scoring-playtest-integration-draft.md)，仅设计，未在本批运行或验证。

修复实际验证：final-scoring-review-red2380通过/6失败，六种未知地区均被错误接受；固定身份集合后final-scoring-review-green2386/2386，0失败/跳过，Unity exit0，XML7fbb741ec8eb165dedfb158e4d01ef35310a7ae99da0dbb2f605aa4d645a91a4。本批累计66新增；数值数据不变，无新脚本。

## 执行台账（完整归档）

# SDD ledger — plan: Docs/Plans/2026-10-16-final-scoring-foundation.md

Base: 361b0fcfd4e71a1b3f2399fc3c78f6757cadcf3f
Ruling: 用户要求自行决定、不询问，沿用已指定E:/TwilightStruggle并创建功能分支，不另复制Unity缓存或请求设计批准 — 保持Hub与已运行Demo路径一致；代价是同一工作目录不能并行改其他分支。
Ruling: Windows下手工维护任务brief与ledger，替代技能bash helper；原生测试结果作为证据，不为记账重复全套 — 代价是需自行保证字段完整。
Pre-flight: Task1 produces FinalScoringDefinition ctor used by Task2 loader; signatures match spec, no conflict.
Task 1: active; BASE 361b0fc; brief = plan Task1 + spec验收。
Task 1 RED: native final-scoring-core-red total2346, passed2320, failed26, skipped0, Unity exit2. New pending interfaces throw NotImplementedException; no compilation failures. XML cb0650c375910621770fe0dd1113bec2670f8e1be5bacb11c335b1b87cd1b628.
Task 1 first GREEN attempt: compile error CS0019 (CardId has Equals, no != operator); no XML, not counted as behavioral verification. Corrected comparison to typed Equals.
Task 1: Ruling: crossing-line test expected -27 omitted Cuba enemy-superpower adjacency; approved world map includes both Cuba and Mexico. Correct expected net -28 and final -9, preserve production code — cost if wrong: regional result assertion would encode an incorrect map expectation. First corrected-compile run 2345 pass/1 expectation fail.
Task 1: complete (361b0fc..bb1107203de46e5307cc415b394ce9c8d0440fd3; native final-scoring-core-verified 2346/2346,0 skipped,exit0; metadata passed). Task 2 active; BASE bb1107203de46e5307cc415b394ce9c8d0440fd3; brief = plan Task2, same LoadSubset signature.
Task 2 initial RED attempt had CS1003 from C# string escaping in generated duplicate-key test, no XML; fixed only test literal before real RED.
Task 2 RED: native final-scoring-loader-red-fixed 2380 total,2346 pass,34 fail,0 skipped,exit2; pending loader NotImplementedException confirmed; XML 5068688a6d917b01ee70769095b48de353b513825ee14bb23f45fae4f32d784d.
Task 2: complete (bb11072..5ab6c0e31ec73d462b26dff855bc7dbed3878b3b; native final-scoring-loader-green2380/2380,0 skipped,exit0; metadata143 descriptors/336symbols/643deps passed). Final whole-branch review pending.
Final review: review_final_scoring_foundation, gpt-6-astra high, fixed range361b0fc..5ab6c0e; 0 Critical,1 Important,0 Minor. Important: directly constructed generic six-region definition can replace Europe with unknown and bypass canonical region identity. Accept and fix in one RED/GREEN pass.
Final: Ruling: reviewer declined host eligibility/atomicity/dedup/UI/dynamic effects/player behavior — keep explicitly deferred: this batch pure foundation; no host wiring or player rebuild claim — cost if wrong: cannot yet play a complete final-scoring turn in Demo; next integration draft states remaining gates.
Final: Ruling: pin the six canonical region IDs in the final Core guard as domain identities, alongside existing schema identity restrictions; scoring values remain only in shared JSON — cost if wrong: variants with different region IDs need a distinct definition/contract, intentionally unsupported here.
Final fix RED: final-scoring-review-red2386 total/2380 pass/6 fail/0 skip/exit2; all six custom replacements were incorrectly accepted (expected ArgumentException, got null). XML21bf38e9d06915730b13b043aae69acf290c4382e24bbcb3307f56ef743998bd.
Final: fixed canonical region ID validation — Custom_six_region_definition_cannot_replace_any_base_region (6cases) RED2380pass/6fail→GREEN2386/2386,0 skipped,Unityexit0. final-scoring-review-green XML7fbb741ec8eb165dedfb158e4d01ef35310a7ae99da0dbb2f605aa4d645a91a4. No re-review dispatched; no deferred minors.
