# 时空之门

这是我自己做的第一个较为完整的小游戏。游戏整体还略显粗糙，放出来给大家图一乐，仅供大家一笑。

玩法很直接：在黑洞的追逐下跑酷，收集战利品，再通过传送门换个时代接着跑。主角可以穿越时空，作者的技术暂时还不能，做得不周到的地方还请多包涵。

**[下载 Windows 64 位版 · STP.zip](https://github.com/RiT-SS/STP/releases/latest/download/STP.zip)** · [查看发布页](https://github.com/RiT-SS/STP/releases/latest)

<p align="center">
  <img src="docs/screenshots/main-menu.png" alt="时空之门主界面" width="640">
</p>

## 开始游玩

下载并完整解压 `STP.zip`，进入文件夹，双击 **时空之门.exe**。无需安装 Unity，请保留 exe 旁的全部文件和目录——它们不是来凑数的。

默认窗口为 **1400×560**。窗口可以调整大小，游戏画面保持 **5:2** 比例，多出的区域显示黑边。

## 三个时代，换个地方继续跑

每局先经过 **古代 → 战争 → 中世纪**，之后在另外两个时代中随机选择下一站。平台的样式和高度也会随机变化。穿越没有历史考试，倒是挺考验手速。

### 古代：先从楼阁之间起跑

收集扇子，越过断开的平台与敌人。左上角显示生命和技能状态，右上角显示距离与本关收集进度。

<p align="center">
  <img src="docs/screenshots/ancient.png" alt="古代场景：楼阁、扇子与跑酷平台" width="640">
</p>

### 战争：勋章捡够了，才给开门

传送需求从 **10 个**开始，每经过一张地图增加 **2 个**，依次为 **10、12、14、16……**。达到需求后，后续生成的平台上会出现可进入的传送门。

右上角显示“本关已收集 / 本关需求”。例如 **12/12** 表示本关已经达标。显示达到上限后，额外收集的战利品仍会计入本局总数，放心捡，不白忙。

<p align="center">
  <img src="docs/screenshots/war.png" alt="战争场景与右上角的本关收集进度" width="640">
</p>

### 中世纪：夜景不错，脚下也得看

进入月夜中的城堡街道，收集头盔，留意沿路的敌人、捕兽夹和高低平台。每次换场，本关收集数重新计算，整局战利品继续累计。

<p align="center">
  <img src="docs/screenshots/medieval.png" alt="中世纪场景：月夜城堡与沿路陷阱" width="640">
</p>

## 三位选手，各有一点保命手艺

### 探险家 爱丽丝

遭遇致命伤害时自动复活，恢复至 **1 颗心**，冷却 **30 秒**。复活时会随特效移动回跑道，随后恢复行动。多一次机会，当然还是尽量别用上。

<p align="center">
  <img src="docs/screenshots/alice.png" alt="选人界面中的探险家爱丽丝" width="520">
</p>

<p align="center">
  <img src="docs/screenshots/revive.png" alt="爱丽丝复活时的特效" width="640">
</p>

### 盗贼 燕子

下落时按住 **W / ↑** 可以滑翔，放缓下降速度。名字里有燕子，滞空这件事确实有点本事。

<p align="center">
  <img src="docs/screenshots/swallow.png" alt="选人界面中的盗贼燕子" width="520">
</p>

### 机器人 伯特

按 **A** 开启冲刺，持续 **3 秒**，冷却 **20 秒**。冲刺期间无敌，并随平台高度移动。三秒很短，耍帅够了，发呆不够。

<p align="center">
  <img src="docs/screenshots/bert.png" alt="选人界面中的机器人伯特" width="520">
</p>

切换场景时，角色技能和道具状态会重置。

## 黑洞与道具：后面在拆，前面在捡

身后的黑洞会吞噬地形。地板和非生物障碍会裂成碎片，被逐渐吸入；生物障碍保持整体消失。欣赏拆迁现场可以，停下来围观就不太建议了。

<p align="center">
  <img src="docs/screenshots/black-hole.png" alt="地形和非生物障碍在黑洞前碎裂" width="640">
</p>

道具在地图预设位置有 **25%** 的概率出现，路过不保证有货。各类道具效果如下：

| 道具 | 效果 |
| --- | --- |
| 护盾 | 提供短暂保护，持续 **5 秒** |
| 磁体 | 吸引附近的道具与战利品，持续 **5 秒** |
| 生命道具 | 补充 **1 颗心** |

<p align="center">
  <img src="docs/screenshots/shield.png" alt="角色身上的护盾效果与左上角的道具状态图标" width="640">
</p>

## 结算与游戏记录

每局战利品从零开始计算。结算会显示整局距离，以及扇子、勋章和头盔三类战利品的数量。穿越一圈，看看自己到底带回了多少东西。

“游戏记录”保存每局的开始时间、所选角色、距离和三类战利品数量。按**距离从长到短、同距离时开始时间从新到旧**排序，最多保留 **20 条**，可以滚动查看。

下载包不附带开发者的历史记录，第一次打开时列表为空。榜单留给你，作者就不在里面占位置了。后续记录保存在游戏目录中的 `SaveData` 文件夹。

<p align="center">
  <img src="docs/screenshots/records.png" alt="首次启动时为空的游戏记录界面" width="640">
</p>

## 操作：手忙脚乱之前先认一下键

| 按键 | 功能 |
| --- | --- |
| W / ↑ | 跳跃、二段跳；盗贼下落时按住可滑翔 |
| S / ↓ | 滑铲、加速下落 |
| A | 机器人冲刺 |
| Esc | 暂停 / 继续；关闭记录页 |
| R | 重新开始 |

截图选自开发过程中的游戏画面，部分界面细节可能与发行版略有不同；机制数值以本文说明为准。

<details>
<summary><strong>想看看它怎么跑起来的？源码工程与编译方法</strong></summary>

本仓库保存我从编译版本恢复、并持续修改的 Unity 可编辑工程。之前换设备把原工程弄丢了，现在总算想起备份这回事，大家别学。

### 在 Unity 中打开

- Unity 版本：**2021.3.21f1c1（中国版）**。
- 在 Unity Hub 中添加仓库根目录，等待首次资源导入完成。
- 打开 `Assets/Scenes/StartMenu.unity`，点击播放按钮。

### 编译 Windows 64 位发行版

使用对应 Unity 编辑器，传入以下参数：

```text
-batchmode -quit -projectPath <工程目录> -executeMethod BuildPlayableRelease.Run -releasePath <新的输出目录>
```

输出目录需要尚不存在。构建会包含五个场景和仓库根目录中的 `游玩说明.txt`；分享发行包时应排除个人的 `SaveData` 目录。

| 目录 | 内容 |
| --- | --- |
| `Assets/Scenes` | 主菜单、选人界面与三个游戏场景 |
| `Assets/Scripts/Assembly-CSharp` | 游戏逻辑 |
| `Assets/Editor` | 构建、编辑器辅助与验证工具 |
| `Assets` 其余目录 | 图片、音频、字体、动画、预制体等资源 |
| `Packages` / `ProjectSettings` | 依赖和项目配置 |
| `docs/screenshots` | README 使用的游戏截图 |

完整资源及 `.meta` 文件均纳入版本管理。缓存、日志、个人游玩记录和编译产物不进入源码仓库；可运行版通过 [Releases](https://github.com/RiT-SS/STP/releases) 提供下载。

</details>
