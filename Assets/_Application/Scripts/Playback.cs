﻿using GMTK2020.Audio;
using GMTK2020.Data;
using GMTK2020.Rendering;
using GMTK2020.TutorialSystem;
using GMTK2020.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace GMTK2020
{
    public class Playback : MonoBehaviour
    {
        [SerializeField] private BoardRenderer boardRenderer = null;
        [SerializeField] private ScoreRenderer scoreRenderer = null;
        [SerializeField] private Button runButton = null;
        [SerializeField] private Button retryButton = null;
        [SerializeField] private BoardManipulator boardManipulator = null;
        [SerializeField] private ChainCounter chainCounter = null;
        [SerializeField] private LevelCounter levelCounter = null;

        // TODO: This is probably not the best place to put this data.
        [SerializeField] private int baseScore = 100;

        private ScoreKeeper scoreKeeper;
        private Simulator simulator;
        private TutorialManager tutorialManager;

        private int turnCount;
        private bool reactionStarted;
        private bool gameEnded;

        public void Initialize(Simulator simulator)
        {
            this.simulator = simulator;

            tutorialManager = TutorialManager.Instance;

            scoreKeeper = new ScoreKeeper(baseScore);
            scoreRenderer.SetScoreKeeper(scoreKeeper);
            chainCounter.SetMaxCracks(simulator.CracksPerChain);
            chainCounter.RenderInitialChain();

            boardManipulator.LastToolUsed += OnLastToolUsed;

            KickOffGameplayLoop();
        }

        private void OnDestroy()
        {
            boardManipulator.LastToolUsed -= OnLastToolUsed;
        }

        public void StartReaction()
        {
            reactionStarted = true;
        }

        private async void KickOffGameplayLoop()
        {
            await RunGameplayLoopAsync();
        }

        private async Task RunGameplayLoopAsync()
        {
            turnCount = 0;

            while (true)
            {
                reactionStarted = false;

                await ShowInteractiveTutorialsAsync();

                await Awaiters.Until(() => reactionStarted || gameEnded);

                boardManipulator.LockPredictions();

                if (gameEnded)
                    break;

                await levelCounter.IncrementTurns().Completion();

                int previousLevel = simulator.DifficultyLevel;

                await PlayBackReactionAsync();

                if (simulator.DifficultyLevel > previousLevel)
                    await levelCounter.IncrementLevel().Completion();

                await StartNewTurn();

                if (!simulator.FurtherMatchesPossible() && !boardManipulator.AnyToolsAvailable())
                {
                    EndGame();
                    break;
                }
            }
        }

        private async Task ShowInteractiveTutorialsAsync()
        {
            int gameCount = TutorialManager.GetGameCount();

            switch (gameCount)
            {
            case 1:
                await ShowFirstGameTutorialSequenceAsync();
                break;
            case 2:
                await ShowSecondGameTutorialAsync();
                break;
            }
        }

        private async Task ShowFirstGameTutorialSequenceAsync()
        {
            switch (turnCount)
            {
            case 0:
                await tutorialManager.ShowTutorialIfNewAsync(TutorialID.OpenVials);
                await tutorialManager.ShowTutorialIfNewAsync(TutorialID.StartReaction);
                break;
            case 1:
                await tutorialManager.ShowTutorialIfNewAsync(TutorialID.SelectSwapTool);
                await tutorialManager.ShowTutorialIfNewAsync(TutorialID.SwapTiles);
                await tutorialManager.ShowTutorialIfNewAsync(TutorialID.PredictChains);
                await tutorialManager.ShowTutorialIfNewAsync(TutorialID.StartChainReaction);
                break;
            case 2:
                await tutorialManager.ShowTutorialIfNewAsync(TutorialID.SelectRemoveTool);
                await tutorialManager.ShowTutorialIfNewAsync(TutorialID.RemoveTileTool);
                await tutorialManager.ShowTutorialIfNewAsync(TutorialID.EndOfMainTutorial);
                break;
            }
        }

        private async Task ShowSecondGameTutorialAsync()
        {
            if (turnCount > 0)
                return;

            await tutorialManager.ShowTutorialIfNewAsync(TutorialID.OmittingVialsIntro);
            await tutorialManager.ShowTutorialIfNewAsync(TutorialID.OmittingVials);
            await tutorialManager.ShowTutorialIfNewAsync(TutorialID.StartReactionWithOmittedVials);
        }

        private async Task PlayBackReactionAsync()
        {
            while (true)
            {
                SimulationStep step = simulator.SimulateNextStep();

                scoreKeeper.ScoreStep(step);
                // We might need to tie this into the board renderer 
                // to sync the update with the match animation.
                scoreRenderer.UpdateScore();

                if (step is MatchStep matchStep)
                {
                    await boardManipulator.RewardMatches(matchStep);
                }
                else if (step is CleanUpStep cleanUpStep)
                {
                    if (cleanUpStep.InertTiles.Count(tile => tile.Marked) > 0)
                        await ShowIncorrectPredictionsTutorialAsync(cleanUpStep.InertTiles);

                    if (cleanUpStep.CrackedTiles.Count > 0)
                        await ShowCrackedVialsTutorialAsync(cleanUpStep.CrackedTiles);
                }
                
                await boardRenderer.AnimateSimulationStepAsync(step);

                if (step.FinalStep)
                    break;
            }
        }

        private async Task ShowIncorrectPredictionsTutorialAsync(HashSet<Tile> inertTiles)
        {
            var inertRects = inertTiles
                .Where(tile => tile.Marked)
                .Select(tile => new GridRect(tile.Position))
                .ToList();

            await tutorialManager.ShowTutorialIfNewAsync(TutorialID.IncorrectPredictions, inertRects);
        }

        private async Task ShowCrackedVialsTutorialAsync(HashSet<Tile> crackedTiles)
        {
            var crackedRects = crackedTiles
                .Select(tile => new GridRect(tile.Position))
                .ToList();

            await tutorialManager.ShowTutorialIfNewAsync(TutorialID.CrackingVials, crackedRects);
        }

        private async Task ShowDifficultyTutorialAsync()
        {
            await tutorialManager.ShowTutorialIfNewAsync(TutorialID.DifficultyIncrease);
        }

        private async Task StartNewTurn()
        {
            ++turnCount;

            chainCounter.SetMaxCracks(simulator.CracksPerChain);

            await boardRenderer.AnimateNewTurn();

            boardManipulator.MakeToolsAvailable();
            boardManipulator.UnlockPredictions();

            runButton.interactable = true;
        }

        private void OnLastToolUsed()
        {
            if (!simulator.FurtherMatchesPossible())
                EndGame();
        }

        private void EndGame()
        {
            gameEnded = true;

            SoundManager.Instance.PlayEffect(SoundEffect.GameEnded);
            runButton.interactable = false;
            scoreKeeper.UpdateHighscore();
            retryButton.ActivateObject();
        }
    }
}