# 单条描述目录同步工具

> description: 将既有描述的摘要/适用状态同步到所属catalog；预览、完整内存校验和显式SHA写入，不创建条目。

用户要求固定且重复至少三次的流程脚本化，并通过JSON description渐进定位。headline-child-feedback、early-war-play、demo-result-reader三批均手工复制description/status；本次战争批次实际出现catalog摘要不一致，说明此流程应固定。三个目录、ID字段和Data独立status语义已经保存在Tools/Configuration/metadata-validation.json，继续复用，不另建静态映射。

采用独立sync-catalog-entry.py与既有只读校验器组合。默认预览输出选中catalog、描述、条目ID、基础SHA、字段差异和候选SHA；--write必须给--expected-catalog-sha，与当前文件字节匹配才可能写入。只支持已唯一登记、ID与kind正确的一个描述，模块/脚本同步description和status，数据只同步description，保留业务status。缺条目、重复登记、身份错配不能自动修复。

候选目录在内存替换后必须通过完整validate-project-metadata校验，包括其它目录、字段、入口和数据依赖。为此给Reader/validate追加仅内部可用的byte_overrides参数，原CLI仍只读且接口不变。同步脚本从自身同目录导入固定校验器，不导入JSON给出的入口，不运行Git/网络/游戏。完整校验出现其它异常时零写入。

写入前复核选中catalog、描述、配置和workflow-policy原始字节未变，所有路径仍通过现有根内/无链接检查。输出使用UTF-8/LF/两空格格式，保留未知catalog字段和其它条目；采用同目录独占临时文件、flush/fsync和os.replace。失败清理仅本工具创建的临时文件，不删除其它数据。不承诺在恶意并发路径替换/硬链接环境中提供安全沙箱；调用时项目输入应保持稳定。

不选择自动同步全目录，因为它可能覆盖尚未审阅的其它修改；不把写入加入原验证CLI，因为只读核验应继续保持清晰。同步成功只证明候选元数据通过当次校验，不证明C#编译、游戏规则或Git已上传。

用户已要求自主继续且不用询问；本设计和执行方法由当前会话决定，沿用原Unity工作目录，建立独立feature分支。两项已有用户未跟踪文件保持不动。完成后真实CLI测试、既有48项校验器回归、项目元数据、一次只读工具审查、提交/推送并核对SHA。
