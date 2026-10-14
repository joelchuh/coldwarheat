# 两张即时 DEFCON/VP 事件的可玩接线

承接八张纯影响力事件。目标：默认早期手牌 Duck and Cover / Nuclear Test Ban 使用真实 Event 入口，显示结算与终局；How I Learned 的等待等级选择仍未实现，不误启用。沿用用户自主开发与常规 Git 授权，本人实施，一次全分支最终独立 review。

设计：增加与纯影响力目录并列的 PracticeDefconVpCatalog（绑定同一 Cards、拒绝跨目录重复 ID），DefconVpEffectDefinition 保留顺序/公式/奖励归属/变更量。DefconVpEventService 复用 DefconService/VictoryService，输入可信定义、当前 DEFCON/VP、EffectController、PhasingPlayer、明确 NoActiveInterference，返回不可变结果和实际步骤。共享数据/Schema验证后只装载两种 OrderedDefconVpEvent；读取已存在参数，不复制业务数值。没有随机、影响力或军事副作用。

先核验 live VP/DEFCON。Duck 先降 DEFCON，核战以 Phasing 判负，立即截断且无 VP 奖励；存活后固定 US 获 5-新等级。Test Ban 先 Controller 得旧等级-2 VP并检查胜利，存活才改善2并按共享上限夹限。记录顺序和是否执行VP步骤，以区分合法0分与核战跳过。终局保留源牌 Resolving、ActiveCard和根槽，非终局按星号去向完成一槽。源牌/轨道/VP/胜负/根推进由 Host 单次发布，失败无中间状态。当前没有选择或外部插入，故每个即时事件一次原子提交；未来完整可恢复事件栈另行接入。

- [x] T1 RED/GREEN：纯定义/加载器/服务；真实Schema三牌全体验证但仅返回两牌，来源/绑定/枚举/步骤/错误参数门禁；两侧控制者与根责任分离、各合法DEFCON、零VP、上限、核战先停、VP先胜不改善、已终局与溢出拒绝、immutable计划。
- [x] T2 RED/GREEN：ActionPracticeDefinition目录保留/同Cards/Risk必需与跨目录冲突门禁；PlanEvent分类分发，两牌实际出牌、非法权限/不持牌/未配置、终局牌去向、版本/序号溢出零变化，现有政变收据/军事/RNG/国家/太空保留；公共DTO、幂等/终局重发与控制器复用Event。
- [x] T3 RED/GREEN：WithPracticeDefconVpEvents在Influence之后/QuickStart之前装配，前后复制不丢目录；默认Duck→Test Ban真实两槽、旧八牌仍可用、未知牌拒绝；Editor/Player共用参数和Schema，新增2份构建载荷。UI显示DEFCON与VP步骤、终局原因；独立Builds/DefconEventPlaytest保留旧窗口。
- [x] T4 全套测试与构建/载荷/初始渲染，维护模块JSON、Wiki、工作记录与报告；一次最终review，阻塞项一轮RED/GREEN修复，minor记录；commit/ff/push和远程SHA核验。真实鼠标与截图证据分开。

Review focus: Duck的固定US收益与Phasing核战责任不混淆；Test Ban必须先旧DEFCON算分再改善且VP终局截断；无活动效果前提、目录互斥与共享值来源真实成立；合法0分不同于未执行VP；保留政变/军事收据与RNG；终局源牌/根槽不收尾；重复/非法/溢出不产生部分轨道或VP变化；不启用第三张待选择事件。

BASE: 5d000704cf1f4c595f2d6a3d52a1f86e48e44cae
