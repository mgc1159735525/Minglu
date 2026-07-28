# 《明路》UI 演示图说明

## 参考方向

- 《美少女梦工厂》系列的核心启发是：人物立绘作为主界面情绪中心，状态与行动按钮围绕两侧展开。
- 《Princess Maker 2 Regeneration》官方说明强调养成模拟中参数需要随时可见，玩家应能一眼判断角色状态。
- 《Princess Maker 2》的资料说明其核心玩法是通过日程安排影响属性、性格、技能与未来结果，因此《明路》的学院界面也以“本周状态 + 行动选择 + 角色反馈”为中心。
- 本批演示图只参考排版结构与信息层级，不使用原作素材、不复刻原作画面。

参考链接：

- https://store.steampowered.com/app/2311530/Princess_Maker_2_Regeneration/
- https://en.wikipedia.org/wiki/Princess_Maker_2
- https://www.playground.ru/gallery/princess_maker_2/
- https://en.riotpixels.com/games/princess-maker-2/screenshots/

## 演示图清单

- `00_ui_mockup_overview.png`：全部界面总览。
- `01_title.png`：标题界面，右侧竖向主菜单，保留主角立绘作为第一视觉。
- `02_create_character.png`：创建角色，性格和特性用卡片选择，显示已选数量。
- `03_academy_home.png`：学院主界面，左侧状态，中间人物，右侧行动，底部短日志。
- `04_event_choice_popup.png`：事件选择弹窗，右上角关闭，但核心事件要求玩家做选择。
- `05_relationship_personality_popup.png`：角色属性、心态、性格、特质、关系统一放入弹窗。
- `06_newspaper_stance.png`：报纸与政治立场，文章阅读和立场变化并列展示。
- `07_strategy_map.png`：战略地图，地图为主体，右侧保留军令简报和关键按钮。
- `08_battle_confirm.png`：战棋攻击确认，攻击前显示双方预估伤害与反击。

## UI 原则

- 主界面不展示表格数据量，只展示玩家当前需要决策的信息。
- 弹窗右上角固定关闭按钮，复杂信息只在玩家点开对应按钮后出现。
- 按钮分组、小型化，避免所有按钮横向铺满。
- 状态用等级、短标签和进度条表达，减少大段说明。
- 事件弹窗保留明确选择结果提示，避免自动跳过事件。
