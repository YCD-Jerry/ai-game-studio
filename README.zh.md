# game-studio

帮助 AI 规划、制作和验证一段可玩的游戏。先说清玩家要完成什么，再沿游戏现有的入口实现和验证。默认一个会话即可，任务能独立推进时再安排分工。

[English](README.md)

## 安装技能

从本仓库的工作副本复制整个 [game-studio 目录](skills/game-studio/)到目标游戏项目。随后在该项目里打开工具，调用技能。

| 工具 | 项目内安装位置 | 调用 |
|---|---|---|
| Codex | `.agents/skills/game-studio/` | `$game-studio` |
| Claude Code | `.claude/skills/game-studio/` | `/game-studio` |

复制时一起保留 `SKILL.md`、`references/`、`templates/`、`assets/` 和 `scripts/`。安装位置见 [Codex 文档](https://developers.openai.com/codex/skills/)和 [Claude Code 文档](https://code.claude.com/docs/en/skills)。

## 从一个任务试起

> 用 game-studio 复现一扇只有读档后才打不开的门。在独立工作副本里走游戏现有入口，确认问题后修复，再复跑这个用例和正常流程。

从[技能入口](skills/game-studio/SKILL.md)可以找到[项目配置](skills/game-studio/references/project-setup.md)、[玩法验收示例](skills/game-studio/references/genre-profiles.md)和[验证指南](skills/game-studio/references/verification.md)。验收示例覆盖叙事、动作、策略和竞技玩法。控件、引擎配置、验收标准和测试命令以当前项目为准。

## 可选 Unity 工具

[Unity 适配指南](skills/game-studio/references/unity-adapter.md)涵盖场景生成、引用扫描、独立 PlayMode 测试和共享编辑器工作流。使用共享编辑器前先看[编辑器安全指南](skills/game-studio/references/editor-safety.md)。需要安装守卫脚本和编辑器状态信标时，先关闭目标项目的 Unity 编辑器，再在本仓库用 Python 3.10 或更新版本执行以下命令。

```sh
python skills/game-studio/scripts/setup_unity.py install --project /path/to/game --editor-closed
python skills/game-studio/scripts/setup_unity.py doctor --project /path/to/game
```

打开目标编辑器后再次运行 `doctor`，可检查实时状态探针。可选的工具钩子覆盖特定工具调用。共享编辑器守卫和项目工作流负责整个操作。

[Unity 钥匙与门示例](examples/unity-key-door/README.md)演示如何在 PlayMode 中进入可玩场景、使用钥匙、存档、读档并完成目标。按示例说明单独运行即可。仓库的 Python 测试可在根目录用 `python -B -m unittest discover -s tests -v` 运行，需要 Python 和 Git。

[对照实验方案](benchmarks/README.md)说明怎样比较不使用技能、使用上一版和使用候选版的效果。

## 从哪里来

这个技能源于《绿野仙踪：黄砖路》，一款改编自鲍姆 1900 年原著的 3D 绘本网页游戏。项目实际开发了 15 天，做出九个可玩的章节，记录了 13 轮试玩反馈。[案例](skills/game-studio/references/case-study.md)记录了制作过程。[踩坑记录](skills/game-studio/references/pitfalls.md)写明了故障现象、根因和有效的检查方法。

作者是 Jerry [@ycdjry](https://github.com/ycdjry)。灵感来自 Sac 的《小王子：星辰之旅》及其公开制作记录。[项目网站](https://littleprince.dev)
