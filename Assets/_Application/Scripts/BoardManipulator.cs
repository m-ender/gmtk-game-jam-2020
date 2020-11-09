﻿using GMTK2020.Data;
using GMTK2020.Rendering;
using GMTK2020.UI;
using GMTKJam2020.Input;
using RotaryHeart.Lib.SerializableDictionary;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace GMTK2020
{
    public class BoardManipulator : MonoBehaviour
    {
        [SerializeField] private BoardRenderer boardRenderer = null;
        [SerializeField] private SerializableDictionaryBase<Tool, Button> toolButtons = null;
        [SerializeField] private Button rotCWButton = null;
        [SerializeField] private Button rotCCWButton = null;
        [SerializeField] private RotationButton rotate2x2Button = null;
        [SerializeField] private RotationButton rotate3x3Button = null;

        public Tool ActiveTool { get; private set; }

        private InputActions inputs;

        private Simulator simulator;
        private Board board;
        private bool initialized = false;
        private bool predictionsFinalised = false;

        private bool isDragging = false;
        private Vector2Int draggingFrom;

        private void Awake()
        {
            inputs = new InputActions();

            inputs.Gameplay.Select.performed += OnSelect;
            inputs.Gameplay.Select.canceled += OnRelease;

            ActiveTool = Tool.ToggleMarked;
        }

        private void OnEnable()
        {
            inputs.Enable();
        }

        private void OnDisable()
        {
            inputs.Disable();
        }

        private void OnDestroy()
        {
            inputs.Gameplay.Select.performed -= OnSelect;
        }

        private void Update()
        {
            if (isDragging)
                OnDrag();
        }

        public void Initialize(Board initialBoard, Simulator simulator)
        {
            board = initialBoard;
            this.simulator = simulator;

            initialized = true;
        }

        public void UseTool(ToolHolder toolHolder)
        {
            SimulationStep step = null;

            switch (toolHolder.Tool)
            {
            case Tool.ShuffleBoard:
                step = simulator.ShuffleBoard();
                break;
            }

            if (step != null)
                KickOffAnimation(step);

            ActiveTool = Tool.ToggleMarked;
            UpdateUI();
        }

        public void ToggleTool(ToolHolder toolHolder)
            => ToggleTool(toolHolder.Tool);

        public void ToggleTool(Tool tool)
        {
            if (tool == ActiveTool)
                ActiveTool = Tool.ToggleMarked;
            else
                ActiveTool = tool;

            UpdateUI();
        }

        public void LockPredictions()
        {
            predictionsFinalised = true;

            gameObject.SetActive(false);
        }

        public void UnlockPredictions()
        {
            predictionsFinalised = false;

            gameObject.SetActive(true);
        }

        private void OnSelect(InputAction.CallbackContext obj)
        {
            if (!initialized || predictionsFinalised)
                return;

            Vector2 pointerPos = inputs.Gameplay.Point.ReadValue<Vector2>();
            
            Vector2Int? gridPosOrNull = ActiveTool == Tool.Rotate2x2
                ? boardRenderer.PixelSpaceToHalfGridCoordinates(pointerPos)
                : boardRenderer.PixelSpaceToGridCoordinates(pointerPos);

            if (gridPosOrNull is null)
                return;

            Vector2Int gridPos = gridPosOrNull.Value;

            if (ActiveTool == Tool.SwapTiles || ActiveTool == Tool.SwapLines)
            {
                isDragging = true;
                draggingFrom = gridPos;
            }
            else
                UseActiveTool(gridPos);
        }

        private void OnDrag()
        {
            Vector2 pointerPos = inputs.Gameplay.Point.ReadValue<Vector2>();

            Vector2Int? gridPosOrNull = boardRenderer.PixelSpaceToGridCoordinates(pointerPos);

            if (gridPosOrNull is null)
            {
                isDragging = false;
                return;
            }

            Vector2Int gridPos = gridPosOrNull.Value;

            int delta = (gridPos - draggingFrom).sqrMagnitude;

            if (delta == 0)
                return;

            isDragging = false;

            if (delta > 1)
                return;

            UseSwapTool(draggingFrom, gridPos);
        }

        private void OnRelease(InputAction.CallbackContext obj)
        {
            if (!initialized || predictionsFinalised)
                return;

            isDragging = false;
        }

        private void UseActiveTool(Vector2Int gridPos)
        {
            SimulationStep step = null;

            switch (ActiveTool)
            {
            case Tool.ToggleMarked:
                TogglePrediction(gridPos);
                break;
            case Tool.RemoveTile:
                step = simulator.RemoveTile(gridPos);
                break;
            case Tool.RefillInert:
                bool wasInert = simulator.RefillTile(gridPos);
                if (!wasInert)
                    return;
                boardRenderer.RefillTile(gridPos);
                break;
            case Tool.Bomb:
                step = simulator.RemoveBlock(gridPos);
                break;
            case Tool.RemoveRow:
                step = simulator.RemoveRow(gridPos.y);
                break;
            case Tool.RemoveColor:
                step = simulator.RemoveColor(board[gridPos].Color);
                break;
            case Tool.Rotate2x2:
                step = simulator.Rotate2x2Block(gridPos, rotate2x2Button.RotationSense);
                break;
            case Tool.Rotate3x3:
                if (!board.IsInBounds(gridPos - Vector2Int.one) || !board.IsInBounds(gridPos + Vector2Int.one))
                    return;

                step = simulator.Rotate3x3Block(gridPos, rotate3x3Button.RotationSense);
                break;
            case Tool.CreateWildcard:
                break;
            }

            if (step != null)
                KickOffAnimation(step);

            ActiveTool = Tool.ToggleMarked;
            UpdateUI();
        }

        public void UseBoardRotation(RotationSenseHolder rotSenseHolder)
        {
            if (ActiveTool != Tool.RotateBoard)
                return;

            SimulationStep step = simulator.RotateBoard(rotSenseHolder.RotationSense);

            KickOffAnimation(step);

            ActiveTool = Tool.ToggleMarked;
            UpdateUI();
        }

        private void UseSwapTool(Vector2Int from, Vector2Int to)
        {
            SimulationStep step;
            switch (ActiveTool)
            {
            case Tool.SwapTiles:
                step = simulator.SwapTiles(from, to);
                break;
            case Tool.SwapLines:
                if (from.x == to.x)
                    step = simulator.SwapRows(from.y, to.y);
                else
                    step = simulator.SwapColumns(from.x, to.x);
                break;
            default:
                return;
            }

            KickOffAnimation(step);

            ActiveTool = Tool.ToggleMarked;
            UpdateUI();
        }

        private async void KickOffAnimation(SimulationStep step)
        {
            await boardRenderer.AnimateSimulationStepAsync(step);
        }

        private void UpdateUI()
        {
            foreach ((Tool tool, Button button) in toolButtons)
                UpdateButtonColor(button, tool);
        }

        private void UpdateButtonColor(Button button, Tool tool)
        {
            // TODO: Move this to a polymorphic component on the button?

            ColorBlock colors = button.colors;
            Color color = ActiveTool == tool ? Color.grey : Color.white;
            colors.normalColor = color;
            colors.selectedColor = color;
            button.colors = colors;

            if (tool == Tool.RotateBoard)
            {
                rotCWButton.gameObject.SetActive(ActiveTool == tool);
                rotCCWButton.gameObject.SetActive(ActiveTool == tool);
            }
        }

        private void TogglePrediction(Vector2Int pos)
        {
            Tile tile = board[pos];

            // TODO: Play a sound effect, maybe do a little animation on the vial
            if (tile.Inert)
                return;

            tile.Marked = !tile.Marked;

            boardRenderer.UpdatePrediction(tile);
        }
    }
}