# 战棋单位整帧序列规范

这里存放战棋单位的完整绘制帧源图。生成器优先读取这里的源图，再输出到：

`Assets/Resources/Art/BattleUnits/<unit_id>/<anim>_<frame>.png`

## 美术基准

单位服装必须是“拿破仑时期军装融合明朝中国元素”。

- 拿破仑时期元素：短军装外套、立领、肩章、斜挎皮带、弹药盒、军靴、军团色边饰、火枪/炮兵装备结构。
- 明朝中国元素：发髻或中式军帽、甲片、护心镜、云肩或护臂、中式腰封、团纹、龙纹/云纹/火纹装饰。
- 义勇军：蓝、黑、红为主，偏地方新军。
- 禁军：红、金、黑为主，偏正规精锐。
- 贼徒：破旧军装残件、旧皮带、破斗篷和布甲混搭。
- 信徒：红褐、金、黑，带护符、经幡、仪式纹样，但仍保留同时代军装轮廓。

不得画成纯明代武将、纯欧洲拿破仑兵、奇幻怪物或现代军服。

## 推荐源图格式

AI 生成或手绘生产时，推荐每个动作单独一张图：

```text
DataTables/battle_unit_sequence_sources/
  swordsmen_volunteers/
    idle.png     6 帧，建议 3 列 x 2 行
    move.png     12 帧，建议 4 列 x 3 行
    attack.png   8 帧，建议 4 列 x 2 行
    hit.png      8 帧，建议 4 列 x 2 行
    recover.png  8 帧，建议 4 列 x 2 行
    defeat.png   10 帧，建议 5 列 x 2 行
```

每一格都必须是一张完整小立绘，不要拆头、躯干、胳膊、腿再拼。

## 动作要求

- 待机：轻微呼吸和重心变化，落点是常规架势。
- 移动：必须表现左右腿交替，并至少有两帧过脚/并脚重叠姿势。
- 攻击：包含预备、发力、命中、收招。
- 受击：包含冲击、后仰、踉跄、失衡结束。
- 回复：从攻击或受击后的失衡状态自然回到常规待机架势。
- 消灭：从失衡、跪倒、倒地到最终倒地轮廓，不要突然消失。

## 导入命令

导入动作源图并重新生成游戏资源：

```powershell
python Tools\import_battle_unit_sequence_sheet.py --sheet D:\art\swordsmen_move_grid.png --unit swordsmen_volunteers --anim move --columns 4 --rows 3
```

如果需要连续导入多个动作，可以先加 `--no-regenerate`，全部源图放好后再运行：

```powershell
python Tools\generate_battle_unit_sprites.py
python Tools\audit_battle_unit_art.py
```

完整 12 列 x 6 行大表只允许给人工严格对齐的源文件使用，不推荐用 AI 一次生成整单位大表，因为容易切出空帧或半截角色。
