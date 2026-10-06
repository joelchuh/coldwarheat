# 冷战早期：印刷卡牌定义

> description: 冷战早期卡牌编号、稳定ID、标题、OPS、阵营及印刷标记的派生浏览表。
> 唯一来源：[cards.json](../../../Data/Cards/deluxe-2015/cards.json)，content_version=0.8.0；英文卡图按PDF页码核对。表中星号表示事件正常结算后移出；下划线仅作显示提醒，不定义效果持续时间。
> 所有效果尚未实现；中国牌不进入普通时代牌库。

| 编号 | CardId（card.省略） | 标题 | OPS | 阵营 | 移出星号 | 下划线 | PDF页 |
|---:|---|---|---:|---|---|---|---:|
| 1 | asia_scoring | Asia Scoring | — | None | 否 | 否 | 2 |
| 2 | europe_scoring | Europe Scoring | — | None | 否 | 否 | 2 |
| 3 | middle_east_scoring | Middle East Scoring | — | None | 否 | 否 | 2 |
| 4 | duck_and_cover | Duck and Cover | 3 | US | 否 | 否 | 2 |
| 5 | five_year_plan | Five Year Plan | 3 | US | 否 | 否 | 2 |
| 7 | socialist_governments | Socialist Governments | 3 | USSR | 否 | 否 | 2 |
| 8 | fidel | Fidel | 2 | USSR | 是 | 否 | 2 |
| 9 | vietnam_revolts | Vietnam Revolts | 2 | USSR | 是 | 否 | 3 |
| 10 | blockade | Blockade | 1 | USSR | 是 | 否 | 3 |
| 11 | korean_war | Korean War | 2 | USSR | 是 | 否 | 3 |
| 12 | romanian_abdication | Romanian Abdication | 1 | USSR | 是 | 否 | 3 |
| 13 | arab_israeli_war | Arab-Israeli War | 2 | USSR | 否 | 否 | 3 |
| 14 | comecon | COMECON | 3 | USSR | 是 | 否 | 3 |
| 15 | nasser | Nasser | 1 | USSR | 是 | 否 | 3 |
| 16 | warsaw_pact_formed | Warsaw Pact Formed | 3 | USSR | 是 | 是 | 3 |
| 17 | de_gaulle_leads_france | De Gaulle Leads France | 3 | USSR | 是 | 是 | 4 |
| 18 | captured_nazi_scientist | Captured Nazi Scientist | 1 | Neutral | 是 | 否 | 4 |
| 19 | truman_doctrine | Truman Doctrine | 1 | US | 是 | 否 | 4 |
| 20 | olympic_games | Olympic Games | 2 | Neutral | 否 | 否 | 4 |
| 21 | nato | NATO | 4 | US | 是 | 是 | 4 |
| 22 | independent_reds | Independent Reds | 2 | US | 是 | 否 | 4 |
| 23 | marshall_plan | Marshall Plan | 4 | US | 是 | 是 | 4 |
| 24 | indo_pakistani_war | Indo-Pakistani War | 2 | Neutral | 否 | 否 | 4 |
| 25 | containment | Containment | 3 | US | 是 | 否 | 5 |
| 26 | cia_created | CIA Created | 1 | US | 是 | 否 | 5 |
| 27 | us_japan_mutual_defense_pact | US/Japan Mutual Defense Pact | 4 | US | 是 | 是 | 5 |
| 28 | suez_crisis | Suez Crisis | 3 | USSR | 是 | 否 | 5 |
| 29 | east_european_unrest | East European Unrest | 3 | US | 否 | 否 | 5 |
| 30 | decolonization | Decolonization | 2 | USSR | 否 | 否 | 5 |
| 31 | red_scare_purge | Red Scare/Purge | 4 | Neutral | 否 | 否 | 5 |
| 32 | un_intervention | UN Intervention | 1 | Neutral | 否 | 否 | 5 |
| 33 | de_stalinization | De-Stalinization | 3 | USSR | 是 | 否 | 6 |
| 34 | nuclear_test_ban | Nuclear Test Ban | 4 | Neutral | 否 | 否 | 6 |
| 35 | formosan_resolution | Formosan Resolution | 2 | US | 是 | 是 | 6 |
| 103 | defectors | Defectors | 2 | US | 否 | 否 | 6 |

[返回数据索引](../map-card-data.md)

[第一批5张影响力事件参数与效果设计](../early-influence-batch1.md)已建立；印刷字段未改变，事件仍未实现。

[第二批3张移除事件](../early-influence-batch2.md)：额度分配、每国上限与按游戏时代移除；短缺边界按用户确认的项目解释处理。

[第三批2张影响力事件](../early-influence-batch3.md)：指定名单内追平与东欧非美国控制国放置。

[持续效果首批3牌](../persistent-effects.md)：越南起义、遏制政策、红色恐怖／清洗；生命周期与OPS修正仍为设计。

[北约与美日安保设计](../treaty-protection.md)：永久保护、前置事件及国家例外接缝。

[马歇尔计划与华沙条约](../alliance-prerequisites.md)：北约前置事件、不同国家放置与互斥分支。

[戴高乐与勃兰特](../nato-exceptions.md)：固定影响力／VP、北约局部例外与取消关系。

朝鲜战争、阿以战争、印巴战争参数已绑定，见[战争事件](../war-events.md)。不代表运行处理器存在；戴维营完整已知参数已绑定，Flower处罚时机/UN边界待裁定；详见战争牌持续与取消。

[影响力迁移](../influence-relocation.md)已绑定#33已知参数；同国移出移入待裁定，处理器planned。

[手牌事件](../hand-events-batch1.md)绑定#5/#10；计分牌可被五年计划弃置，无额外计分。

台湾决议#35见[动态计分](../scoring-effects-batch1.md)，只改计分事实不改基础地图。

本时代计分牌已按[七张计分牌绑定](../scoring-cards.md)直接引用地区定义；正式处理器仍planned。

本批参数与边界见[space-events-batch1](../space-events-batch1.md)；C#待实现。

[UN双牌使用](../un-intervention.md)绑定#32；普通OPS和配对事件分开，未知来源停止。
