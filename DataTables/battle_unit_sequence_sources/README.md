# 战棋单位整帧序列规范

这里存放战棋单位的“完整绘制帧”源图。生成器会优先读取这里的源图，再输出到：

`Assets/Resources/Art/BattleUnits/<unit_id>/<anim>_<frame>.png`

## 推荐格式

每个单位一个文件夹，每个动作一张横向动作条：

```text
DataTables/battle_unit_sequence_sources/
  swordsmen_volunteers/
    idle.png     6 帧
    move.png     12 帧
    attack.png   8 帧
    hit.png      8 帧
    recover.png  8 帧
    defeat.png   10 帧
```

每一格都要是一张完整小立绘，不要拆头、躯干、胳膊、腿再拼。

如果单行动作条太挤，可以改成网格。例如移动 12 帧推荐 4 列 3 行：

```powershell
python Tools\import_battle_unit_sequence_sheet.py --sheet D:\art\swordsmen_move_grid.png --unit swordsmen_volunteers --anim move --columns 4 --rows 3
```

工具会写入同名 JSON，切帧顺序为从左到右、从上到下。

## 绘制要求

- 角色朝向、头部、躯干、脚步方向必须一致。
- 移动帧要表现左右腿交替：触地、抬腿、过渡、另一腿触地。
- 攻击帧要包含预备、发力、命中、收招。
- 受击帧要包含冲击、后仰、稳定。
- 回复帧要从受击或行动后自然回到待机。
- 被消灭帧要从失衡、倒下到消失或倒地结束。
- 背景使用纯色绿幕 `#00ff00`，角色内部不要使用同色。
- 不要文字、水印、边框、编号。

## 一键导入

导入某个动作条并重新生成游戏资源：

```powershell
python Tools\import_battle_unit_sequence_sheet.py --sheet D:\art\swordsmen_move.png --unit swordsmen_volunteers --anim move
```

导入完整大表并重新生成游戏资源：

```powershell
python Tools\import_battle_unit_sequence_sheet.py --sheet D:\art\swordsmen_full.png --unit swordsmen_volunteers
```

完整大表默认为 12 列 6 行，行顺序：

`idle / move / attack / hit / recover / defeat`

旧的 4 行 6 列表只会读取完整的 6 帧待机，不再把 6 帧循环成 12 帧移动。
