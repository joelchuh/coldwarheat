# ColdWarStruggle

《冷战热斗》电脑端学习项目，GitHub 仓库名称为 coldwarheat，内部项目代号为 ColdWarStruggle。

## 当前状态

本仓库保存工程约定、Wiki、Git 教程和项目 Skill。远程仓库由用户创建，地址为 https://github.com/joelchuh/coldwarheat，当前为公开仓库。

本地项目目录为 E:\TwilightStruggle，目录名称与远程仓库名可以不同。本地 .git 已初始化，但因执行环境写入故障，尚未完成本地首次提交、origin 关联或与远程历史同步。此次线上提交通过 GitHub 网页完成，不代表本地执行过 git push。

尚未创建 Unity 工程、游戏源码或运行游戏测试。

## 已确认的方向

- Unity 6 + URP + C#；已在本机发现编辑器 6000.6.0f1。尚未生成 ProjectSettings 或锁定包版本。
- 纯 C# 规则核心，独立于 Unity、UI、网络和美术资源。
- 官方 Deluxe 基础规则作为研究基准；首版不启用可选规则、扩展和自定义平衡。精确资料版本、勘误及卡牌清单在规则设计阶段锁定。
- 当前先细化全部规则设计，再逐步实施；本地双人优先，真实联网和 AI 后置。
- 代码、规则文档、Wiki、工作记录和项目 Skill 保存在同一仓库，便于版本同步。

## 从哪里开始

1. 阅读 [AGENTS.md](AGENTS.md) 了解工程边界和当前阶段。
2. 从 [Wiki 索引](Docs/Wiki/index.md) 的摘要定位主题。
3. 阅读相关知识点，再按类或函数定位必要源码及测试；当前尚无源码。
4. 初始化经过见 [本地工作记录](Docs/Worklogs/2026-09-16-repository-bootstrap.md)，线上保存状态见 [GitHub 工作记录](Docs/Worklogs/2026-09-17-github-bootstrap.md)。

## Git 入门

见 [Git 工作流与操作理由](Docs/Wiki/git-workflow.md)。Unity 的缓存、构建输出和个人配置不纳入版本管理；Assets 及其 .meta、Packages 和 ProjectSettings 在创建后需要纳入版本管理。

这里是项目准备仓库，不是已可运行的游戏。请勿把编辑器安装文件复制进仓库。
