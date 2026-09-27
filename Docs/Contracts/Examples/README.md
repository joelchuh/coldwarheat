# 契约结构样例

本目录的五个fixture文件只演示结构，不是正式地图、卡牌或美术。国家名、编号和多数数值为合成数据；资源键没有对应图片或模型，事件没有实现。

- map/cards演示结构与类型分支。
- layout/theme演示同一规则ID如何连接位置和外观。
- manifest使用map/cards样例的真实文件SHA-256和约定的DataDigest算法，但缺少正式规则和setup且审核pending，禁止用于创建对局。

按[契约目录](../catalog.json)选择Schema；[案例文件](../../Design/map-card-contracts.cases.json)中的edits为结构检查输入描述，不是任意脚本，也不授予运行权限。执行验证结果见[本次工作记录](../../Worklogs/2026-09-27-map-card-contracts.md)。
