# game-studio · 一人 + AI 团队做 3D 网页游戏

**让一组 Claude Code 会话像游戏工作室一样协作，做出一款能在浏览器里玩的 3D 游戏。**

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

- `SKILL.md`：一页说完：角色、每章的生产循环、16 条硬规矩、阅读指南。
- `references/`：13 份参考：团队分工、生产循环、质量线、排版与界面（导演的对齐审美和拖拽排版工具）、美术流水线、角色、建筑道具、Unity 架构、编辑器安全、30 条踩坑、黑客松一日计划、发布、案例。
- `templates/`：每个会话的开场消息、CLAUDE.md、TEAM.md、PRD、章节剧本、任务书、试玩反馈、晨报、夜间计划、开发日志、恢复文档、美术任务板，以及 11 段图像和审图提示词。
- `assets/unity/`：可以直接拷进项目的 C# 起点（已对照 Unity 6 编译通过）：代码生成场景、剧情导演、TSV 本地化、交互键、手持道具挂点、运行时 NavMesh、审图机位截图、编辑器状态信标、一键网页构建。
- `scripts/`：守卫脚本、编辑器锁、Play 状态探针、交还编辑器、一轮完整的编辑器作业（带总期限）、对话框看门狗，以及一个 Claude Code 钩子：导演在 Play 时，任何会话往 Assets 写文件都会被直接拦下。

## 安装

在 Claude Code 里作为插件安装：

```
/plugin marketplace add ycdjry/ai-game-studio
/plugin install game-studio@ai-game-studio
```

或者直接复制 skill 文件夹（二选一，别两种都装）：

```bash
git clone https://github.com/ycdjry/ai-game-studio
cp -r ai-game-studio/skills/game-studio ~/.claude/skills/
```

## 怎么用

直接描述你想做的游戏就行，例如：

- 「把《彼得·潘》做成一个能在浏览器里玩的小型 3D 探索游戏。」
- 「给我的 Unity 游戏搭一个 Claude 团队：每关一个会话，美术一个。」
- 「明天黑客松，8 个小时，我想做一个好看、能玩的 3D 场景。」
- 「两个会话在同一个 Unity 项目里老是把对方的编译搞坏。」

完整项目从 `SKILL.md` 的 “Start a project” 开始；一天的比赛看 `references/hackathon.md`。

## 需要什么

- Claude Code（最好能同时开几个会话）。
- Unity 6（带 Web Build Support），以及会话能调用的 Unity 命令行（`unity command ...`）或 MCP。没有命令行时，守卫可以改用“信标模式”。
- Python 3（给拦截钩子用，可选但推荐）。
- Blender 4 以上（无头脚本）。
- 一个图像模型（比如 Codex 里的 GPT 图像）和一个 AI 3D 工具（比如 Tripo），账号由你自己掌握。Claude 从不登录、不付费，这些都由你来做。

## 致谢

灵感来自 Sac 的《小王子：星辰之旅》（[littleprince.dev](https://littleprince.dev)）和他公开的制作流程（Opus、Tripo、Blender、Unity）。由 @雷霆俊叶 [@ycdjry](https://github.com/ycdjry) 与 Claude 一起完成。
