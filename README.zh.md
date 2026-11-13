# game-studio · 一人 + AI 团队做 3D 网页游戏

**规划、制作和验证一段可玩的游戏。从单会话开始，任务适合并行时再引入分工。**

一个人当导演；一个“总指挥”会话负责派活和审核；工人会话各管一章或一个系统；图像 AI 画概念图，AI 3D 工具做角色，Blender 脚本做建筑，Unity（URP、WebGPU）组装成游戏。这个 skill 是整套打法：分工、规矩、模板、脚本，以及教会我们这些规矩的 30 个错误。

[English](README.md)

## 从哪里来

从《绿野仙踪：黄砖路》蒸馏而来。这是一款改编自 1900 年鲍姆原著的 3D 绘本网页游戏（原著，不是电影：银鞋，不是红宝石鞋）。

| | |
|---|---|
| 用时 | 15 天 |
| 团队 | 1 个人类导演，约 20 个 Claude Code 会话（最多约 8 个同时在跑），图像 AI，Tripo，Blender |
| 产出 | 9 章，17 个由代码生成的场景，约 7.2 万行 C#，约 4 万行 Blender Python |
| 过程 | 1325 次提交，356 张图的订单，13 轮试玩共 256 条编号反馈，16 篇每日开发日志 |

真正有意思的不是速度，而是组织方式：每个 AI 先读的团队说明、每个文件只有一个主人、多个会话共用一台编辑器靠锁排队、场景全部由代码生成、试玩反馈按导演原话编号，以及从每次代价高昂的失误里长出来的规矩。

## 里面有什么

- `SKILL.md`：入口包含规则优先级、制作与验证流程，以及按任务读取的参考文件。
- `references/`：参考文件覆盖项目配置、验证、团队分工、生产循环、质量线、排版与界面（导演的对齐审美和拖拽排版工具）、美术流水线、角色、建筑道具、Unity 架构、编辑器安全、35 条踩坑、黑客松一日计划、发布、网页按章加载（先实测再拆包，网页和编辑器不一样时怎么查）、展示片（从运行中的游戏逐帧录制、穿越机镜头、卡小节剪辑、导出核验）、案例。
- `templates/`：每个会话的开场消息、CLAUDE.md、TEAM.md、PRD、章节剧本、任务书、试玩反馈、晨报、夜间计划、开发日志、恢复文档、美术任务板，以及 11 段图像和审图提示词。
- `assets/unity/`：可以直接拷进项目的 C# 起点（已对照 Unity 6 编译通过）：代码生成场景、剧情导演、TSV 本地化、交互键、手持道具挂点、运行时 NavMesh、审图机位截图、编辑器状态信标、一键网页构建。
- `scripts/`：守卫脚本、编辑器锁、Play 状态探针、交还编辑器、一轮完整的编辑器作业（带总期限）、对话框看门狗、断引用检查（构建前场景引用的生成资产必须在 git 里存在）、裁切对照图、展示片工具（`scripts/showreel/`：Play 里逐帧存图、示例镜头、剪成中间片、样张、音乐对位核验、私人信息扫描），以及一个 Claude Code 钩子：导演在 Play 时，任何会话往 Assets 写文件都会被直接拦下。

## 安装

从本仓库的工作副本复制整个 [game-studio 目录](skills/game-studio/) 到项目下：

| 工具 | 安装位置 |
|---|---|
| Codex | `.agents/skills/game-studio/` |
| Claude Code | `.claude/skills/game-studio/` |

复制后，`SKILL.md` 应和 `references/`、`scripts/` 同级。在该项目里打开工具，Codex 用 `$game-studio`，Claude Code 用 `/game-studio`。安装位置见 [Codex 文档](https://developers.openai.com/codex/skills/)和 [Claude Code 文档](https://code.claude.com/docs/en/skills)。

## 先用一个任务试试

> 用 game-studio 复现一扇只有读档后才打不开的门。在独立工作副本里跑项目已有的冒烟测试，说明哪些通过了，哪些还需要真人试玩。

从 [SKILL.md](skills/game-studio/SKILL.md) 开始。[项目配置指南](skills/game-studio/references/project-setup.md)说明当前决定、引擎设置和画风放在哪里。[验证指南](skills/game-studio/references/verification.md)说明怎样用引用扫描、同机位截图、运行时检查和试玩取得证据。Unity 工具按需使用。运行时测试依赖项目自己的场景、测试数据和断言。

在仓库根目录运行 `python -B -m unittest discover -s tests -v` 可以复测引用检查器。需要 Python 和 Git。

## 致谢

灵感来自 Sac 的《小王子：星辰之旅》（[littleprince.dev](https://littleprince.dev)）和他公开的制作流程（Opus、Tripo、Blender、Unity）。由 @雷霆俊叶 [@ycdjry](https://github.com/ycdjry) 与 Claude 一起完成。
