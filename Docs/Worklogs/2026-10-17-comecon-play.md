# 经互会运行工作记录

description：记录经互会既有规则的增量实现、边界、测试及实际Player状态。

Base a4920a1faaab11ff22dc6d4eb7c856b6209a1dc0，feat/comecon-play。前批迁移代码/构建/审查已push，鼠标与简短反馈Minor未完成。当前系统09:14更新重启提示阻挡UI；不操作系统更新，保存检查点后继续规则工作。

brainstorming/writing-plans/executing-plans沿用用户自主授权，不重复询问。采用既有AddToDistinct的封闭控制过滤，避免重复宿主/新DSL。计划和接口见对应Architecture/Plans。

Ruling: EI3-RULING-01保持RequiresRuling，其他牌短缺解释不推广。若后续确认减量，只修改对应参数/策略及新增测试。
Ruling: 旧Any模式保留原候选与变化顺序；新增过滤模式排序发出事件，避免改变已有调用语义。

规则核对：GMT TS_Cards_Deluxe.pdf搜索结果英文visual_element card14确认四个非美国控制东欧国家各加一点；未用旁列译文、旧版示例或其错误对局推导新裁定。来源见Architecture。

Task1 RED: comecon-core-red 3684/3657/27，0跳过，XML SHA d83a09db688d75f8739b88a8ca26a8d826c3bbf816481328b89421a0f9af2aba。18 loader stub失败，9过滤/顺序/短缺/未知enum失败；41新增用例中14覆盖已有机制已通过。实现后再跑全量，未以测试夹具编译错误充当RED。

Task1 GREEN: comecon-core-green 3684/3684，0失败/跳过/不确定，162937ms，XML db469309c5d6d93692b4f222494a702da64a01e3712704f0c8580ea457088ac6。41新增核心/装载用例；旧Any行为与既有3643测试保持通过。新模式稳定排序，所有数量仍从参数读取。
Ruling: Task1部署门禁复用既有共享回归，本批新增验证未知干预/终局；不为无改动门禁重复夹具，代价为单卡部署专项覆盖暂未增加。

Task1 checkpoint d6355b80d94825cbf14fd3d0c2fdd041f253f411已push，ls-remote与HEAD一致。元数据181描述/87数据/471符号/741依赖通过。Task2在此BASE开始，完整出牌、会话及Demo测试与stub已写，等待首次原生运行。

Task2 RED: comecon-play-red 3728/3711/17，44新增中27既有通用生命周期/会话分支已通过；17 Demo stub明确NotImplemented，XML ded9502d1590fa408df67f3c34458dba4d0c712789e9beface0b534ca03b2e0c。无需新增逐卡Host，接线复用CountrySet。

Task2 GREEN: comecon-play-green 3728/3728，0失败/跳过，173526ms，XML 852f6a4f035f5ddabdd4adb8549b08bc967f5157909eb51591447fb30261ffc9。85项本批新增全部通过。GREEN后修正Editor新增方法插入导致的菜单属性挂接，恢复迁移菜单并单独注册经互会菜单；只影响编辑器入口，由下一次原生构建编译并执行，不重复全套规则测试。
Ruling: OPS已接受放置后把合法国家减至3，FinishInfluence返回RequiresRuling且保留已接受前缀与Resolving根；不提供自创裁定/回滚已接受操作，代价是该未决边界当前不能继续。
Ruling: 原Unity目录及功能分支继续使用；新Player独立目录保留旧构建，用户未跟踪文件不纳入提交。代价为目录占用，利于恢复。
补验收旧迁移Player：系统遮挡自行消失，已记录苏联实际迁移/零迁移/原出牌重发；没有修改OS安全或更新设置。
