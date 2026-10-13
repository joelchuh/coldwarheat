# 元数据只读校验

> description: 校验模块/数据/脚本目录与描述、真实入口及数据依赖的一致性；查询命令、只读边界、失败码和实际验证证据。
> 状态：正式Python3.11+工具，Windows Python3.12.14测试。C#符号仅词法匹配，不能替代编译或规则测试。

先读[工具description](../../Tools/Descriptors/validate-project-metadata.script.json)，确认输入/前置条件，再运行：

~~~powershell
python Tools/Scripts/validate-project-metadata.py --project-root E:\TwilightStruggle
~~~

请使用已安装Python3.11+的实际可执行文件；python不在PATH时用绝对路径，不自动安装环境。CLI只输出有界JSON：status=passed且退出0才表示本次元数据检查通过。失败退出1并返回error_code/必要定位信息。它不保存文件；如需报告，由调用方显式重定向到新结果文件。

共享[配置](../../Tools/Configuration/metadata-validation.json)保存三个catalog/描述目录及限额；其[data描述](../../Data/Descriptors/metadata-validation.data.json)说明版本。必备字段唯一来源仍为workflow-policy，不在脚本复制。模块/脚本catalog.status与描述一致；Data catalog业务状态与描述可读状态不同，只分别校验非空。catalog数据ID匹配dataset_id，不误用描述自身id。

~~~mermaid
flowchart LR
 A[共享配置 + 现有workflow-policy] --> B[三catalog]
 B --> C[描述字段/ID/摘要/状态]
 C --> D[入口文件/C#词法符号/数据依赖]
 D --> E[只读JSON报告]
~~~

检查包含描述漏登、ID重复、摘要不一致、必备字段缺失、入口缺失、C#词法符号缺失和data_dependencies未知。planned_entrypoints不作为可执行入口，不要求未来文件已存在。输入文件及目录路径必须在根内；拒绝绝对子路径、..、UNC根和符号链接/重解析点；相对子路径每一段不得以点或空格结尾。.cs/.json扩展名按大小写不敏感识别，避免Windows文件别名漏检。不会import/eval或运行入口、访问网络、写Git、自动修复任何内容。

配置启动读取上限64KiB/深度16；配置后的JSON、源码、描述数和目录条目限额从共享配置读取，并受工程硬上限约束。字节数在读取前后均检查。JSON重复键、NaN、过深/过大及错误形状明确失败。调用期间路径/文件应稳定；工具不是并发替换攻击或硬链接的安全沙箱，也不认证游戏规则数据的内容/来源。

常见失败码：CATALOG_MISMATCH表示ID/description或适用status不一致；DESCRIPTOR_INVENTORY_MISMATCH表示目录漏登；MISSING_PATH/MISSING_SYMBOL用于入口定位；UNKNOWN_DATA_DEPENDENCY用于引用；INVALID_PATH/LINK_REJECTED用于路径边界；INVALID_JSON/DUPLICATE_KEY/JSON_DEPTH/FILE_TOO_LARGE用于格式和限额。失败停止依赖步骤，不自动补值。

[测试description](../../Tools/Descriptors/validate-project-metadata.tests.script.json)对应：

~~~powershell
python Tools/Tests/validate-project-metadata.tests.py
~~~

每次创建新临时fixture根，执行48个真实子进程/文件系统案例并保留summary和全部产物。不删除任何目录。另有本批两次真实NTFS Junction检查，分别验证根目录和入口路径绕路拒绝；这两项不冒称由上述Python测试创建。详细证据见[报告](../Testing/metadata-validator.validation.json)。

脚本化依据为deterministic-random、initial-card-deal、dealt-setup-demo、defcon-core、military-core、victory-core六个独立批次；具体Worklog入口在工具JSON。满足用户至少三次且将复用的要求。每批更新描述后运行本工具，再运行该批真正需要的游戏/脚本测试，随后审阅与授权Git同步。
