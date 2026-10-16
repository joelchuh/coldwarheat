# 单条描述目录同步 Implementation Plan

> **For agentic workers:** 使用executing-plans在当前会话实现；完成后按requesting-code-review做一次fresh只读整批工具审查。

**Goal:** 将反复手工复制的catalog摘要同步变成可预览、有边界、可复用的工具。
**Architecture:** 既有共享配置定位→构造单entry候选→完整元数据内存校验→显式SHA保护→原子替换一个catalog。
**Tech Stack:** Python3.11+标准库、既有元数据校验器、Windows本地文件。
**Spec:** Docs/Architecture/catalog-entry-sync.md。

## Global Constraints

- 不新增目录静态映射，读取Tools/Configuration/metadata-validation.json和workflow-policy。
- 默认只预览；--write需要--expected-catalog-sha和--expected-candidate-sha；不创建/删除/改ID/移动条目。
- 不运行JSON描述入口、Git或网络；只写指定catalog及短暂同目录临时文件。
- Data.status业务语义独立；模块/脚本status匹配描述。
- 输入路径稳定、根内、无链接；保留两项用户未跟踪文件。

## Review Focus

1. 多处catalog异常：只选一项时其它错误必须阻止写入；真实CLI负例。
2. Data的业务status与描述不同：仅改description；真实写入与第二次幂等测试。
3. 校验后输入改变：比较原始字节，写前失败无变更；SHA冲突及注入并发测试。
4. 失败临时文件：replace异常时原catalog完整，只清理自己的临时文件；文件系统断言。
5. 入口JSON含路径/代码：始终只读验证不执行，测试入口包含失败哨兵。

## Task 1 — 同步CLI、边界与知识索引

**Files:** 新Tools/Scripts/sync-catalog-entry.py、Tools/Tests/sync-catalog-entry.tests.py及各自Tools/Descriptors描述；修改validate-project-metadata.py的Reader/validate可选内存覆盖、Tools/Descriptors/catalog.json、Docs/Wiki/index.md。新Wiki/Worklog/Testing报告，引用既有共享配置data_id workflow.metadata_validation。

**Interfaces:** `plan_sync(project_root_value, descriptor_relative)`返回内部候选计划；`synchronize(project_root_value, descriptor_relative, write=False, expected_catalog_sha=None, expected_candidate_sha=None)`返回有界JSON结果；CLI `--project-root --descriptor [--write --expected-catalog-sha --expected-candidate-sha]`。内部validate(root_value,byte_overrides=None)保持旧调用兼容。

- [x] 写可运行CLI桩与真实子进程测试，先确认缺实现RED。覆盖module/script/data预览写入、幂等、元数据不完整、身份/登记错误、路径越界/链接、大小/重复键、SHA冲突、无关条目保留、非法CLI、其它目录不一致和异常清理。
```python
before = catalog.read_bytes()
preview = invoke(root, descriptor)
assert catalog.read_bytes() == before
written = invoke(root, descriptor, '--write', '--expected-catalog-sha', preview['catalog_sha256'], '--expected-candidate-sha', preview['candidate_sha256'])
assert written['status'] == 'updated'
assert read_catalog()['entries'][1] == unrelated_entry
```
- [x] 实现严格读取、唯一条目、两字段候选、完整overlay校验、原始字节重核与同目录原子写入。无差异不重写。
```python
candidate_bytes = json.dumps(candidate, ensure_ascii=False, indent=2).encode('utf-8') + b'\n'
validation = metadata.validate(str(root), byte_overrides={catalog_relative: candidate_bytes})
```
- [x] 运行新工具测试和既有48项校验器回归；保留实际结果与临时fixture，不删除测试产物。运行真实项目预览并确认Git无意外修改。
- [x] 为工具/测试配套JSON描述、共享数据依赖、Wiki流程、工作日志和验证报告；元数据与diff通过后提交，fresh只读审查，修复必要问题，推送并ls-remote核对。源码只涉及Python和文档，不重复运行未改变的Unity测试。
