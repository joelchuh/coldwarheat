# Git 工作流与操作理由

> description: 理解本地仓库和 GitHub 的区别，查找提交、同步、Unity 忽略规则及当前恢复步骤。
> 状态（2026-09-27）：E:\TwilightStruggle 的 main 跟踪 origin/main。地区计分与太空竞赛设计已上传；连接故障与核验见 [同步恢复记录](../Worklogs/2026-09-27-git-sync-recovery.md)。

## 三个位置

工作目录保存正在编辑的文件；目录内的 .git 保存本地版本历史；GitHub 保存可同步的线上历史。连接插件不会自动创建本地关联，网页提交也不等于执行了本地 push。

- 本地项目：E:\TwilightStruggle。
- 远程仓库：https://github.com/joelchuh/coldwarheat ，由用户创建，当前为公开仓库。
- Unity 编辑器：E:\unity6.6，安装文件不属于项目源码。

## 常用命令及理由

| 命令 | 用途 |
|---|---|
| git status --short | 检查哪些文件被修改、哪些尚未跟踪 |
| git diff | 审阅尚未暂存的差异 |
| git add -- 文件路径 | 选择本次版本要保存的文件 |
| git diff --cached | 审阅即将提交的内容 |
| git commit -m 消息 | 在本地保存一个版本，离线也能进行 |
| git fetch origin | 下载远程历史，不替换工作文件 |
| git push | 上传本地提交 |
| git log -1 --oneline | 查看最近一笔提交及编号 |

commit 和 push 是两步；只有上传并核对成功，才能说已经同步 GitHub。

## Unity 文件取舍

跟踪 Assets 及其 .meta、Packages、ProjectSettings；忽略 Library、Temp、Logs、UserSettings 和构建输出。这样保留可重建工程所需的数据，同时避免缓存挤占仓库。大型美术资源加入前再评估 Git LFS。

.gitattributes 统一文本换行并标识二进制文件，减少无意义差异。不把令牌、密码、私钥或个人环境文件放进仓库。

## 每次工作的闭环

先查 Docs/Wiki/index.md，再查对应知识点和相关函数。完成修改后，运行相关验证，更新 Wiki 的代码符号、流程和测试入口，并在 Docs/Worklogs 保存真实结果。随后审阅差异、提交、同步。不覆盖用户已有修改，不强制推送。

```mermaid
flowchart LR
    A[Wiki 摘要定位] --> B[读取相关代码]
    B --> C[修改与验证]
    C --> D[更新知识点和工作记录]
    D --> E[审阅并 commit]
    E --> F[push 并核对远程]
```

## 本地恢复记录

用户在 CMD 中成功克隆远程仓库，将旧目录改名为 E:\TwilightStruggle-backup-20260919，再将克隆目录改名为正式目录。接入时核验本地 HEAD、origin/main 与 GitHub main 均为 e3f30f0e9939e61d4eceaca1bd267c5ee8d7a168，工作区干净。此后新提交以实际 Git 历史为准。

备份保留，不自动删除。网页提交、插件权限与本机 Git 登录是不同通道；某个通道成功不代表其余通道可用。

## 连接完成后的常规同步

提交者身份只配置在本项目，使用 joelchuh 和 GitHub 隐私邮箱，不改全局配置。接入线上历史后首次 push 可使用 git push -u origin main，-u 用于记录跟踪关系。网页、插件、命令行登录状态可能不同，身份验证由用户完成，不在聊天里收集令牌。

```powershell
git push
git status -sb
git log -1 --oneline
git ls-remote origin refs/heads/main
```

核对本地 HEAD 与远程 main 的提交编号一致，并单独检查工作区是否有未提交修改。失败时保留本地提交，明确报告尚未同步。
## Git 直连失败、系统代理已启用时

2026-09-27发生过：Git HTTPS直连报连接重置或443连接失败；Windows已启用本机代理，但Git没有继承它。读取系统配置后，仅通过本次命令的http.proxy参数沿用已配置代理，ls-remote和push均成功。

排查与执行边界：

1. 先核对本地状态和远端SHA，保留待上传提交；区分本地运行工具故障、网络错误与认证错误。
2. 仅在连接失败时，读取Windows当前ProxyEnable/ProxyServer及相关环境配置。先确认代理确实启用、地址格式受支持；不硬编码本机端口，不打印代理凭据。带账号信息、按协议分组或PAC配置需要单独处理，不能直接拼接成URL。
3. 对简单、已启用的本机HTTP代理，使用命令级http.proxy作只读ls-remote验证；成功后再使用同一连接设置push。不改系统代理或全局Git设置，不关闭TLS验证。
4. push返回成功后，核对本地HEAD、远端main和工作区；必要时通过GitHub插件独立复核。

这是一次已观察到的环境恢复记录，不保证所有网络问题都由代理导致。当前未新增永久脚本；以后达到工作流策略的复用条件，再将输入校验、超时和错误分类固化，并配套JSON描述。来源是本次实际命令结果，非游戏规则。
