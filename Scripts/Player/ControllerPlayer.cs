namespace Game
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;
    using Rewired;
    using Mirror;

    /// <summary>
    /// In the gameplay scene, each GameplayClient has one or more ControllerPlayers.
    /// On the host, to support local co-op, multiple people can join as different
    /// ControllerPlayers by using different controllers. Non-host clients are limited
    /// to one ControllerPlayer, which is set up as soon as they enter the gameplay
    /// scene.
    /// </summary>
    public class ControllerPlayer
    {
        public Character character = null;
        public int controllerPlayerIndex;

        private GameplayClient gameplayClient;
        public Rewired.Player rewiredPlayer;

        private PlayerHud hud;

        public enum CharacterState { SelectBase, SelectEyes, SelectHair, SelectHairColor, SelectClass, Playing }

        private CharacterState characterState;
        private CharacterCreationPanel creationPanel;
        private List<Sprite> partsList;
        private int bodyIndex, eyesIndex, hairIndex, hairColorIndex, classIndex;

        public ControllerPlayer(GameplayClient gameplayClient, int controllerPlayerIndex, int rewiredPlayerId)
        {
            this.gameplayClient = gameplayClient;
            this.controllerPlayerIndex = controllerPlayerIndex;
            rewiredPlayer = ReInput.players.GetPlayer(rewiredPlayerId);

            var hudCanvas = ServiceLocator.Get<HudCanvas>();
            hud = hudCanvas.PlayerHuds[gameplayClient.GetHudIndex(controllerPlayerIndex)];
            hud.gameObject.SetActive(true);
            hud.PlayerName.text = gameplayClient.playerName;

            hud.SetGameplayElementsVisible(false);
            hud.SetInventoryPanelVisible(false);
            if (gameplayClient.isLocalPlayer)
            {
                StartCharacterCreation();
            }
        }

        public void StartCharacterCreation()
        {
            hud.SetGameplayElementsVisible(false);
            hud.SetInventoryPanelVisible(false);
            hud.SetCharacterCreationPanelVisible(true);
            creationPanel = hud.CharacterCreationPanel;
            creationPanel.Randomize();
            bodyIndex = GotoState(CharacterState.SelectBase, "Body", creationPanel.CharacterParts.Bodies, creationPanel.BodyImage);
            hud.CharacterCreationPanel.OutfitImage.enabled = false;
        }

        public void OnUpdate()
        {
            switch (characterState)
            {
                case CharacterState.SelectBase:
                    if (UpdateCharacterCreation(creationPanel.BodyImage, ref bodyIndex))
                    {
                        var eyesList = creationPanel.CharacterParts.IsMale(bodyIndex) ? creationPanel.CharacterParts.MaleEyes : creationPanel.CharacterParts.FemaleEyes;
                        eyesIndex = GotoState(CharacterState.SelectEyes, "Eyes", eyesList, creationPanel.EyesImage);
                    }
                    break;
                case CharacterState.SelectEyes:
                    if (UpdateCharacterCreation(creationPanel.EyesImage, ref eyesIndex))
                    {
                        var hairList = creationPanel.CharacterParts.IsMale(bodyIndex) ? creationPanel.CharacterParts.MaleHair : creationPanel.CharacterParts.FemaleHair;
                        hairIndex = GotoState(CharacterState.SelectHair, "Hair", hairList, creationPanel.HairImage);
                    }
                    break;
                case CharacterState.SelectHair:
                    if (UpdateCharacterCreation(creationPanel.HairImage, ref hairIndex))
                    {
                        hairColorIndex = GotoStateColor(CharacterState.SelectHairColor, "Hair Color");
                    }
                    break;
                case CharacterState.SelectHairColor:
                    if (UpdateCharacterCreationColor(creationPanel.HairImage, creationPanel.CharacterParts.HairColors, ref hairColorIndex))
                    {
                        var classList = creationPanel.CharacterParts.IsMale(bodyIndex) ? creationPanel.CharacterParts.MaleOutfits : creationPanel.CharacterParts.FemaleOutfits;
                        classIndex = GotoState(CharacterState.SelectClass, "Class", classList, creationPanel.OutfitImage);
                        hud.CharacterCreationPanel.OutfitImage.enabled = true;
                    }
                    break;
                case CharacterState.SelectClass:
                    if (UpdateCharacterCreation(creationPanel.OutfitImage, ref classIndex))
                    {
                        characterState = CharacterState.Playing;
                        hud.SetGameplayElementsVisible(true);
                        hud.SetCharacterCreationPanelVisible(false);
                        gameplayClient.CmdSpawnCharacter(controllerPlayerIndex, bodyIndex, eyesIndex, hairIndex, hairColorIndex, classIndex);
                    }
                    break;
            }
        }

        private int GotoState(CharacterState state, string category, List<Sprite> list, Image image)
        {
            characterState = state;
            creationPanel.CategoryText.text = category;
            partsList = list;
            var index = list.IndexOf(image.sprite);
            if (index == -1)
            {
                index = 0;
                image.sprite = list[0];
            }
            image.enabled = (image.sprite != null);
            if (state == CharacterState.SelectClass)
            {
                creationPanel.CategoryText.text = creationPanel.CharacterParts.GetClassName(index);
            }
            return index;
        }

        private bool UpdateCharacterCreation(Image image, ref int index)
        {
            if (rewiredPlayer.GetButtonDown(RewiredActions.Previous))
            {
                index = (index == 0) ? partsList.Count - 1 : index - 1;
                image.sprite = partsList[index];
                image.enabled = (image.sprite != null);
                if (characterState == CharacterState.SelectClass)
                {
                    creationPanel.CategoryText.text = creationPanel.CharacterParts.GetClassName(index);
                }
            }
            else if (rewiredPlayer.GetButtonDown(RewiredActions.Next))
            {
                index = (index + 1) % partsList.Count;
                image.sprite = partsList[index];
                image.enabled = (image.sprite != null);
                if (characterState == CharacterState.SelectClass)
                {
                    creationPanel.CategoryText.text = creationPanel.CharacterParts.GetClassName(index);
                }
            }
            return rewiredPlayer.GetButtonDown(RewiredActions.Use);
        }

        private int GotoStateColor(CharacterState state, string category)
        {
            characterState = state;
            creationPanel.CategoryText.text = category;
            return 0;
        }

        private bool UpdateCharacterCreationColor(Image image, List<Color> partsList, ref int index)
        {
            if (rewiredPlayer.GetButtonDown(RewiredActions.Previous))
            {
                index = (index == 0) ? partsList.Count - 1 : index - 1;
                image.color = partsList[index];
            }
            else if (rewiredPlayer.GetButtonDown(RewiredActions.Next))
            {
                index = (index + 1) % partsList.Count;
                image.color = partsList[index];
            }
            return rewiredPlayer.GetButtonDown(RewiredActions.Use);
        }

    }
}
