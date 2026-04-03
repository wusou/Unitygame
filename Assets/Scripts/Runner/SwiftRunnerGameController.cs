using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SwiftRunner
{
    public sealed class SwiftRunnerGameController : MonoBehaviour
    {
        [SerializeField] private float stallKillSpeed = 4.6f;
        [SerializeField] private float stallGraceDuration = 2.1f;
        [SerializeField] private float comboDecayDuration = 2.35f;
        [SerializeField] private float restartDelay = 0.9f;
        [SerializeField] private float speedRecoveryPerSecond = 6.4f;
        [SerializeField] private float wallTargetSpeed = 11.8f;
        [SerializeField] private float slumTargetSpeed = 13.8f;
        [SerializeField] private float marketTargetSpeed = 16.1f;
        [SerializeField] private float wallSectionEnd = 55f;
        [SerializeField] private float slumSectionEnd = 120f;
        [SerializeField] private float slashSpeedBoost = 0.7f;
        [SerializeField] private float stompSpeedBoost = 1.4f;

        private readonly List<SwiftRunnerEnemy> enemies = new();
        private readonly List<SwiftRunnerObstacle> obstacles = new();

        private float[] laneCenters = { -2.6f, 0f, 2.6f };
        private float finishLineX = 194f;
        private float startX = 2f;

        private SwiftRunnerPlayerController player;
        private SwiftRunnerHud hud;

        private int combo;
        private int score;
        private int bestCombo;
        private float comboTimer;
        private float stallTimer;
        private bool gameEnded;

        public void Configure(float[] lanes, float startForwardX, float finishForwardX)
        {
            laneCenters = lanes;
            startX = startForwardX;
            finishLineX = finishForwardX;
        }

        public IReadOnlyList<float> LaneCenters => laneCenters;
        public float StartX => startX;
        public bool GameEnded => gameEnded;

        public void ResetRuntimeState()
        {
            enemies.Clear();
            obstacles.Clear();
            combo = 0;
            score = 0;
            bestCombo = 0;
            comboTimer = 0f;
            stallTimer = 0f;
            gameEnded = false;

            if (Application.isPlaying)
            {
                StopAllCoroutines();
            }
        }

        public void BindPlayer(SwiftRunnerPlayerController runner)
        {
            player = runner;
            hud?.Refresh(combo, score, ResolvePhaseLabel(startX), "A/D 左右移动，W/S 变道，空格二段跳，V 下踩。", ResolveControlSummary(), 0f);
        }

        public void BindHud(SwiftRunnerHud runnerHud)
        {
            hud = runnerHud;
            hud.Refresh(combo, score, ResolvePhaseLabel(startX), "A/D 左右移动，W/S 变道，空格二段跳，V 下踩。", ResolveControlSummary(), 0f);
        }

        public void BindCamera(SwiftRunnerCameraFollow follow)
        {
        }

        public float GetLaneY(int laneIndex)
        {
            return laneCenters[Mathf.Clamp(laneIndex, 0, laneCenters.Length - 1)];
        }

        public void RegisterEnemy(SwiftRunnerEnemy enemy)
        {
            if (enemy != null && !enemies.Contains(enemy))
            {
                enemies.Add(enemy);
            }
        }

        public void RegisterObstacle(SwiftRunnerObstacle obstacle)
        {
            if (obstacle != null && !obstacles.Contains(obstacle))
            {
                obstacles.Add(obstacle);
            }
        }

        public float ResolveTargetRunSpeed(float forwardX)
        {
            if (forwardX < wallSectionEnd)
            {
                return wallTargetSpeed;
            }

            if (forwardX < slumSectionEnd)
            {
                return slumTargetSpeed;
            }

            return marketTargetSpeed;
        }

        public float ResolveRecoverySpeed()
        {
            return speedRecoveryPerSecond;
        }

        public bool TryQuickSlash(SwiftRunnerPlayerController runner, float reach)
        {
            var bestDistance = float.MaxValue;
            SwiftRunnerEnemy target = null;
            for (var index = 0; index < enemies.Count; index++)
            {
                var enemy = enemies[index];
                if (enemy == null || !enemy.IsAlive || enemy.LaneIndex != runner.CurrentLaneIndex)
                {
                    continue;
                }

                var delta = enemy.ForwardX - runner.ForwardX;
                if (delta < 0.1f || delta > reach || delta >= bestDistance)
                {
                    continue;
                }

                bestDistance = delta;
                target = enemy;
            }

            if (target == null)
            {
                return false;
            }

            target.Kill(SwiftRunnerKillMethod.Slash);
            runner.ApplySpeedBoost(slashSpeedBoost);
            RegisterEnemyKill("快刀斩杀");
            return true;
        }

        public bool TryResolveStomp(SwiftRunnerPlayerController runner, out SwiftRunnerEnemy stompedEnemy)
        {
            stompedEnemy = null;

            for (var index = 0; index < enemies.Count; index++)
            {
                var enemy = enemies[index];
                if (enemy == null || !enemy.IsAlive || !enemy.CanBeStompedBy(runner))
                {
                    continue;
                }

                stompedEnemy = enemy;
                enemy.Kill(SwiftRunnerKillMethod.Stomp);
                runner.ApplySpeedBoost(stompSpeedBoost);
                RegisterEnemyKill("踩踏处决");
                return true;
            }

            return false;
        }

        public bool TryGetStompPreview(SwiftRunnerPlayerController runner, out SwiftRunnerEnemy previewEnemy)
        {
            previewEnemy = null;
            var bestDistance = float.MaxValue;

            for (var index = 0; index < enemies.Count; index++)
            {
                var enemy = enemies[index];
                if (enemy == null || !enemy.IsAlive || enemy.LaneIndex != runner.CurrentLaneIndex)
                {
                    continue;
                }

                var delta = Mathf.Abs(enemy.ForwardX - runner.ForwardX);
                if (delta > 1.2f || delta >= bestDistance)
                {
                    continue;
                }

                bestDistance = delta;
                previewEnemy = enemy;
            }

            return previewEnemy != null;
        }

        public void TickRunner(SwiftRunnerPlayerController runner, float deltaTime)
        {
            if (runner == null || gameEnded)
            {
                return;
            }

            comboTimer += deltaTime;
            if (combo > 0 && comboTimer >= comboDecayDuration)
            {
                BreakCombo("连击断了，分数减半。");
            }

            if (runner.CurrentSpeed <= stallKillSpeed)
            {
                stallTimer += deltaTime;
                if (stallTimer >= stallGraceDuration)
                {
                    KillRunner("你慢下来了，追兵扑了上来。");
                    return;
                }
            }
            else
            {
                stallTimer = 0f;
            }

            for (var index = 0; index < obstacles.Count; index++)
            {
                var obstacle = obstacles[index];
                if (obstacle == null)
                {
                    continue;
                }

                obstacle.Resolve(runner);
                if (gameEnded)
                {
                    return;
                }
            }

            for (var index = 0; index < enemies.Count; index++)
            {
                var enemy = enemies[index];
                if (enemy == null || !enemy.IsAlive || !enemy.IsThreatening(runner))
                {
                    continue;
                }

                if (runner.IsSliding)
                {
                    BreakCombo("滑铲穿过人群，连击被打断。");
                    continue;
                }

                KillRunner("被士兵碰到了。");
                return;
            }

            if (runner.ForwardX >= finishLineX)
            {
                CompleteRun();
                return;
            }

            hud?.Refresh(combo, score, ResolvePhaseLabel(runner.ForwardX), ResolveHint(runner.ForwardX), ResolveControlSummary(), ResolvePursuitProgress());
        }

        public void ApplySlowdown(float penalty, string message)
        {
            if (player == null || gameEnded)
            {
                return;
            }

            player.ApplySlowdown(penalty);
            BreakCombo(message);
        }

        public void KillRunner(string reason)
        {
            if (gameEnded)
            {
                return;
            }

            gameEnded = true;
            player?.HandleDeath();
            hud?.ShowStatus(reason, new Color(1f, 0.45f, 0.45f, 1f));
            StartCoroutine(ReloadCurrentScene());
        }

        public void CompleteRun()
        {
            if (gameEnded)
            {
                return;
            }

            gameEnded = true;
            bestCombo = Mathf.Max(bestCombo, combo);
            hud?.ShowStatus($"突围成功！得分 {score}，最高连击 {bestCombo}", new Color(0.96f, 0.88f, 0.32f, 1f));
        }

        public void RegisterEnemyKill(string message)
        {
            comboTimer = 0f;
            stallTimer = 0f;
            combo += 1;
            bestCombo = Mathf.Max(bestCombo, combo);
            score += combo;
            hud?.FlashEvent($"{message}  Combo x{combo}", new Color(0.98f, 0.88f, 0.34f, 1f));
        }

        private void BreakCombo(string reason)
        {
            if (combo <= 0)
            {
                hud?.FlashEvent(reason, new Color(1f, 0.74f, 0.44f, 1f));
                return;
            }

            combo = 0;
            comboTimer = 0f;
            score = Mathf.FloorToInt(score * 0.5f);
            hud?.FlashEvent(reason, new Color(1f, 0.74f, 0.44f, 1f));
        }

        private IEnumerator ReloadCurrentScene()
        {
            yield return new WaitForSeconds(restartDelay);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private string ResolvePhaseLabel(float forwardX)
        {
            if (forwardX < wallSectionEnd)
            {
                return "城墙突围";
            }

            if (forwardX < slumSectionEnd)
            {
                return "贫民窟三车道";
            }

            return "集市终点冲刺";
        }

        private string ResolveHint(float forwardX)
        {
            if (forwardX < wallSectionEnd)
            {
                return "空格可二段跳，V 下踩守军，Shift/Ctrl 加速。";
            }

            if (forwardX < slumSectionEnd)
            {
                return "A/D 左右移动，W/S 变道，V 滑铲，Shift/Ctrl 加速。";
            }

            return "空格二段跳，V 下踩或滑铲，Shift/Ctrl 保持速度。";
        }

        private float ResolvePursuitProgress()
        {
            return Mathf.Clamp01(stallTimer / stallGraceDuration);
        }

        private string ResolveControlSummary()
        {
            return player != null ? player.ControlSummary : "跳跃 Space  下踩 V  加速 Shift  常驻 Ctrl";
        }
    }
}
