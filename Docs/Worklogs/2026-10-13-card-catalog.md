# 卡牌印刷静态目录

基线df3a649，分支feature/card-catalog，按已批准的静态数据/纯C#架构继续持续开发。先读Wiki与JSON，再实现Docs/Plans/2026-10-13-card-catalog.md；没有新增正式工作脚本。

## 实现

- Core新增CardId、CardDefinition、CardEffectBinding/CardParameterReference和CardCatalog；三类卡牌、不可变集合、Ordinal查询、未知ID/枚举与重复拒绝。
- Infrastructure新增LoadPrintedSubset与印刷profile校验；从已提交的cards v0.45.0冻结规范化摘要，原卡牌数据未修改。
- CanonicalJsonDigest复用给地图与卡牌profile；对象键排序，数组顺序保持。
- ContractJson补齐现有cards-v2实际使用的maximum/allOf/if/then/else，预检查所有分支且保留复杂度异常传播。

## 验证记录

1. card-contract-red：580项，575通过/5失败，验证新增关键词尚不支持。
2. card-contract-green：579/580；实现已正确传播ContractLimitException，但NUnit Assert.Throws要求恰好ArgumentException。定位异常堆栈后改为Assert.Catch验证子类，未放松复杂度限制。
3. card-contract-green2：580/580。
4. card-catalog-red：632项，581通过/51失败；新目录与定义尚未实现。
5. card-catalog-green：629/632；实际参数引用使用region-scoring等带连字符的dataset_id，新接口规则过严。保留规范源，修正接口兼容。
6. card-catalog-boundary-red：634项，629通过/5失败；新增精确连字符与尾换行断言。
7. card-catalog-boundary-green：634/634，零跳过，exit0；XML SHA `c54099752ebcc2a0d768f4de502c184083c91bd19a65e6dc222d430fd0313cc4`。

各次XML与SHA、profile基线记录见[报告](../Testing/card-catalog.validation.json)。本批无UI变化，不重建播放器；上批真实部署Demo仍保留。

## 明确裁量与边界

Ruling：印刷行必须全部Verified；效果声明只核对注册来源/定位，允许保留现有19项Pending设计引用。理由：本接口只发布印刷目录，不返回可执行处理器或完整RulePack。影响：后续运行规则包仍需另行处理这些待核实条目，禁止用本目录绕过；Wiki/description已明确。

Ruling：复用小型规范化摘要工具，避免两处profile复制算法；已有地图回归确认原profile仍匹配。该摘要不是发布manifest的原始字节SHA。

没有实现正式发牌、动态牌区、处理器注册、头条或完整对局。Profile冻结此前提交的资料，不等于重新逐张官方认证。

## 收尾

634项测试已通过，105份描述与32/67/6目录、入口符号和.meta已核验。独立审阅通过，没有Critical/Important；一处open_items旧文案已修正并由审阅者复核。进入已授权Git提交推送，实际远程状态单独核验；下一批准备随机状态与牌堆基础。

审阅未判断完整官方卡文正确性、参数目标、handler执行、整局和UI行为；这些保持本批范围外，不声称完成。
