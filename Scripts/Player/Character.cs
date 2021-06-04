namespace Game
{
    using System.Collections.Generic;
    using UnityEngine;
    using Mirror;
    using Com.LuisPedroFonseca.ProCamera2D;

    /// <summary>
    /// The network entity representing a ControllerPlayer's character.
    /// </summary>
    [RequireComponent(typeof(NetworkIdentity))]
    public class Character : NetworkMob
    {

        [SyncVar] public GameObject owner;
        [SyncVar] public int controllerPlayerIndex;

        [SyncVar] public int bodyIndex;
        [SyncVar] public int eyesIndex;
        [SyncVar] public int hairIndex;
        [SyncVar] public int hairColorIndex;
        [SyncVar] public int classIndex;

        [SyncVar(hook = nameof(OnChangeCoins))] public int coins;
        [SyncVar(hook = nameof(OnChangeMana))] public int mana;
        [SyncVar(hook = nameof(OnChangeMaxMana))] public int maxMana;

        [Header("Character Customization Parts")]

        [SerializeField] private CharacterParts characterParts;
        [SerializeField] private CharacterPartPrefabs malePrefabs;
        [SerializeField] private CharacterPartPrefabs femalePrefabs;
        [SerializeField] private GameObject body;

        [Header("Action Settings")]

        [SerializeField] private float walkSpeed = 1f;
        [SerializeField] private float runSpeed = 3f;
        [SerializeField] private float meleeAttackDuration = 0.35f;
        [SerializeField] private float rangedAttackDuration = 0.35f;

        [Header("Info")]

        [SerializeField] private Inventory inventory;
        [SerializeField] private Weapon weapon;
        [SerializeField] private GameObject weaponObject;

        private GameplayClient gameplayClient;
        private Rewired.Player rewiredPlayer;
        private PlayerOverhead overhead;
        public PlayerHud hud;
        private string hudNotificationID;

        private double attackEligibleTime;
        private bool isMale;
        private bool isFemale;

        private const float MeleeAttackDelay = 0.25f;
        private const float RangedAttackDelay = 0.25f;
        private const float ProjectileSpawnOffsetY = 0.35f;
        private const int FistDamage = 1;

        protected override bool MoveOnServer => false;

        public GameplayClient GameplayClient { get { return gameplayClient; } }
        public Interactable CurrentInteractable { get; set; } = null;
        public Inventory Inventory { get { return inventory; } }

        // Called when becomes active on clients. Called for local player and remote player GameObjects.
        // Set up the customized character.
        // Set up the camera if this is the local player.
        public override void OnStartClient()
        {
            base.OnStartClient();
            gameplayClient = owner.GetComponent<GameplayClient>();
            gameObject.name = $"Player {gameplayClient.index} {gameplayClient.playerName} Character";

            if (gameplayClient.isLocalPlayer)
            {
                // If this is the local player, set the camera to follow:
                ProCamera2D.Instance.AddCameraTarget(transform);

                // and get a reference to the input device:
                if (gameplayClient.controllerPlayers != null &&
                    gameplayClient.controllerPlayers.Count > controllerPlayerIndex)
                {
                    gameplayClient.controllerPlayers[controllerPlayerIndex].character = this;
                    rewiredPlayer = gameplayClient.controllerPlayers[controllerPlayerIndex].rewiredPlayer;
                }
            }

            // Set up customized character:
            isMale = characterParts.IsMale(bodyIndex);
            isFemale = !isMale;
            var prefabs = isMale ? malePrefabs : femalePrefabs;
            if (isFemale) bodyIndex -= characterParts.NumMaleBodies;

            var bodyPrefab = prefabs.Bodies[bodyIndex];
            var bodySpriteRenderer = body.GetComponent<SpriteRenderer>();
            var prefabSpriteRenderer = bodyPrefab.GetComponent<SpriteRenderer>();
            var bodySpriteSkin = body.GetComponent<SpriteSkin>();
            var prefabSpriteSkin = bodyPrefab.GetComponent<SpriteSkin>();
            if (isFemale) bodySpriteSkin.folderPath = bodySpriteSkin.folderPath.Replace("Male", "Female");
            bodySpriteRenderer.sprite = prefabSpriteRenderer.sprite;
            bodySpriteSkin.AssignSkin(prefabSpriteSkin.newSprite);

            var eyes = (prefabs.Eyes[eyesIndex] != null) ? Instantiate(prefabs.Eyes[eyesIndex], body.transform) : null;
            Instantiate(prefabs.Outfits[classIndex], body.transform);
            var hair = (prefabs.Hair[hairIndex] != null) ? Instantiate(prefabs.Hair[hairIndex], body.transform) : null;
            if (hair != null) hair.GetComponent<SpriteRenderer>().color = characterParts.HairColors[hairColorIndex];

            inventory = new Inventory();
            weapon = null;

            // Set up HUD:
            var hudIndex = gameplayClient.GetHudIndex(controllerPlayerIndex);
            var hudCanvas = ServiceLocator.Get<HudCanvas>();
            hud = hudCanvas.PlayerHuds[hudIndex];
            hud.gameObject.SetActive(true);
            hud.PlayerName.text = gameplayClient.playerName;
            hud.SetGameplayElementsVisible(true);
            hud.PortraitFace.sprite = bodySpriteRenderer.sprite;
            hud.PortraitEyes.sprite = (eyes != null) ? eyes.GetComponent<SpriteRenderer>().sprite : null;
            hud.PortraitEyes.enabled = (hud.PortraitEyes.sprite != null);
            hud.PortraitHair.sprite = (hair != null) ? hair.GetComponent<SpriteRenderer>().sprite : null;
            hud.PortraitHair.color = characterParts.HairColors[hairColorIndex];
            hud.PortraitHair.enabled = (hud.PortraitHair.sprite != null);
            hud.Initialize(maxHealth, maxMana);
            hudNotificationID = "NotificationDisplay" + hudIndex;

            // Set up overhead status bars:
            overhead = GetComponentInChildren<PlayerOverhead>();
            overhead.PlayerName.text = gameplayClient.playerName;
        }

        private void OnDisable()
        {
            if (gameplayClient.isLocalPlayer && Camera.main != null)
            {
                ProCamera2D.Instance.RemoveCameraTarget(transform);
            }
        }

        public static int StartingHealth(int classIndex)
        {
            return 100;
        }

        public static int StartingMana(int classIndex)
        {
            return 100;
        }

        protected override void OnChangeHealth(int oldValue, int newValue)
        {
            base.OnChangeHealth(oldValue, newValue);
            if (hud == null) return;
            hud.HealthSlider.value = newValue;
        }

        private void OnChangeMana(int oldValue, int newValue)
        {
            if (hud == null) return;
            hud.ManaSlider.value = newValue;
            overhead.UpdateMana((float)newValue / maxMana);
        }

        protected override void OnChangeMaxHealth(int oldValue, int newValue)
        {
            if (hud == null) return;
            hud.HealthSlider.maxValue = newValue;
        }

        private void OnChangeMaxMana(int oldValue, int newValue)
        {
            if (hud == null) return;
            hud.ManaSlider.maxValue = newValue;
        }

        private void OnChangeCoins(int oldValue, int newValue)
        {
            if (hud == null) return;
            hud.CoinsText.text = newValue.ToString();
        }

        [ClientRpc]
        public void RpcShowAlert(string message)
        {
            var notificationData = new Animmal.NotificationSystem.NotificationData()
            {
                Texts = new List<string>(new string[] { message }),
                Sprites = new List<Sprite>(new Sprite[] { })
            };
            Animmal.NotificationSystem.NotificationManager.Instance.ShowNotification(hudNotificationID, notificationData);
        }

        public void ShowAlert(Sprite sprite, string message)
        {
            var notificationData = new Animmal.NotificationSystem.NotificationData()
            {
                Texts = new List<string>(new string[] { message }),
                Sprites = new List<Sprite>(new Sprite[] { sprite })
            };
            Animmal.NotificationSystem.NotificationManager.Instance.ShowNotification(hudNotificationID, notificationData);
        }

        public void HideAlerts()
        {
            Animmal.NotificationSystem.NotificationManager.Instance.HideOfStyle(hudNotificationID);
        }

        protected override void UpdateClient()
        {
            base.UpdateClient();

            if (gameplayClient == null || !gameplayClient.isLocalPlayer || IsDead) return;

            if (rewiredPlayer.GetButtonDown(RewiredActions.Inventory))
            {
                ToggleInventory();
                if (hud.InventoryPanel.IsOpen)
                {
                    HideAlerts();
                }
            }

            if (hud.InventoryPanel.IsOpen)
            {
                hud.InventoryPanel.UpdateClient(this, rewiredPlayer);
            }
            else
            {
                var horizontal = rewiredPlayer.GetAxisRaw(RewiredActions.Horizontal);
                var vertical = rewiredPlayer.GetAxisRaw(RewiredActions.Vertical);
                MoveVector = new Vector2(horizontal, vertical).normalized;

                // If horizontal or vertical axis is above threshold, move:
                if (MoveVector.magnitude > MoveAxisThreshold)
                {
                    var isRunning = !rewiredPlayer.GetButton(RewiredActions.Walk);
                    MoveSpeed = isRunning ? runSpeed : walkSpeed;
                    Animator.SetBool(AnimatorIsRunning, isRunning);
                }

                if (NetworkTime.time >= attackEligibleTime && rewiredPlayer.GetButtonDown(RewiredActions.Attack))
                {
                    if ((weapon != null) && (weapon.ItemType == ItemType.Ranged))
                    {
                        attackEligibleTime = NetworkTime.time + rangedAttackDuration;
                        Animator.SetTrigger(AnimatorShootTrigger);
                        Invoke(nameof(CmdRangedAttack), RangedAttackDelay);
                    }
                    else
                    {
                        attackEligibleTime = NetworkTime.time + meleeAttackDuration;
                        Animator.SetTrigger(AnimatorAttackTrigger);
                        Invoke(nameof(CmdMeleeAttack), MeleeAttackDelay);
                    }
                }


                if (rewiredPlayer.GetButtonDown(RewiredActions.Interact))
                {
                    if (CurrentInteractable != null)
                    {
                        CmdInteract();
                        CurrentInteractable = null;
                    }
                }
            }
        }

        protected override void FixedUpdateClient()
        {
            if (gameplayClient != null && gameplayClient.isLocalPlayer)
            {
                UpdateAnimation();
                base.UpdateMovement();
            }
        }

        [Command]
        public virtual void CmdInteract()
        {
            if (CurrentInteractable != null)
            {
                CurrentInteractable.Interact(gameObject);
            }
        }

        private void ToggleInventory()
        {
            hud.SetInventoryPanelVisible(!hud.InventoryPanel.IsOpen);
            if (hud.InventoryPanel.IsOpen)
            {
                hud.InventoryPanel.Repaint(inventory);
            }
        }

        private Item LoadItem(string itemName)
        {
            if (string.IsNullOrEmpty(itemName)) return null;
            return Resources.Load<Item>($"Items/{itemName}");
        }

        public bool CanAddItem(string itemName)
        {
            return inventory.Items.Count < Inventory.MaxSize;
        }

        [ClientRpc]
        public void RpcAddItem(string itemName, bool showAlert)
        {
            var item = LoadItem(itemName);
            if (item != null && CanAddItem(itemName))
            {
                inventory.Items.Add(item);
                if (showAlert) ShowAlert(item.Icon, item.DisplayName);
            }
        }

        [Command]
        public void CmdEquipItem(string itemName)
        {
            RpcEquipItem(itemName);
        }

        [ClientRpc]
        public void RpcEquipItem(string itemName)
        {
            ClientEquipItem(itemName);
        }

        protected void ClientEquipItem(string itemName)
        { 
            var item = LoadItem(itemName);
            if (item == null) return;
            var itemPrefab = isMale ? item.MalePrefab : item.FemalePrefab;
            var instance = Instantiate(itemPrefab, body.transform);
            instance.GetComponent<SpriteRenderer>().color = item.Tint;
            if (item is Weapon)
            {
                inventory.Equipped.RemoveAll(x => (x.ItemType == ItemType.Melee) || (x.ItemType == ItemType.Ranged));
                if (weaponObject != null) Destroy(weaponObject);
                weapon = item as Weapon;
                weaponObject = instance;
            }
            else
            {
                inventory.Equipped.RemoveAll(x => x.ItemType == item.ItemType);
            }
            inventory.Equipped.Add(item);
            if (hud.InventoryPanel.IsOpen)
            {
                hud.InventoryPanel.Repaint(inventory);
            }
        }

        [Command]
        public void CmdDropItem(int index)
        {
            if (!(0 <= index && index < inventory.Items.Count)) return;

            // Spawn in scene:
            var item = LoadItem(inventory.Items[index].name);
            ServerSpawnPickup(item.name, transform.position + 0.5f * new Vector3(Random.value, Random.value, 0));

            // Remove from inventory:
            RpcDropItem(index);
        }

        [ClientRpc]
        public void RpcDropItem(int index)
        {
            if (!(0 <= index && index < inventory.Items.Count)) return;
            var item = inventory.Items[index];
            var isEquipped = (inventory.Equipped.Find(x => x.name == item.name) != null);
            if (isEquipped && (inventory.Items.FindAll(x => x.name == item.name).Count <= 1))
            {
                if (item is Weapon)
                {
                    inventory.Equipped.Remove(item);
                    if (weaponObject != null) Destroy(weaponObject);
                    weaponObject = null;
                    weapon = null;
                }
            }
            inventory.Items.RemoveAt(index);
            if (hud.InventoryPanel.IsOpen)
            {
                hud.InventoryPanel.Repaint(inventory);
            }
        }

        [Server]
        public void ServerSpawnPickup(string itemName, Vector3 position)
        {
            var item = LoadItem(itemName);
            if (item != null && item.PickupPrefab != null)
            {
                var pickup = Instantiate(item.PickupPrefab.gameObject, position, Quaternion.identity);
                NetworkServer.Spawn(pickup);
            }
        }

        private Collider2D[] hits = new Collider2D[20];

        [Command]
        public void CmdMeleeAttack()
        {
            if (IsDead) return;

            var attackRadius = 0.5f;
            var attackDirection = new Vector3(
                (Mathf.Abs(FacingVector.x) < 0.5f) ? 0 : Mathf.Sign(FacingVector.x),
                (Mathf.Abs(FacingVector.y) < 0.5f) ? 0 : Mathf.Sign(FacingVector.y), 0);
            var attackCenter = transform.position + attackDirection * 0.5f;

#if UNITY_EDITOR
            Debug.DrawRay(attackCenter, attackRadius * Vector3.left, Color.red, 2);
            Debug.DrawRay(attackCenter, attackRadius * Vector3.right, Color.red, 2);
            Debug.DrawRay(attackCenter, attackRadius * Vector3.up, Color.red, 2);
            Debug.DrawRay(attackCenter, attackRadius * Vector3.down, Color.red, 2);
#endif

            var numHits = Physics2D.OverlapCircleNonAlloc(attackCenter, attackRadius, hits);
            for (int i = 0; i < numHits; i++)
            {
                if (hits[i].gameObject == this.gameObject) continue;
                var targetEntity = hits[i].GetComponent<NetworkEntity>();
                if (targetEntity != null)
                {
                    //Debug.Log("Hit " + targetEntity);
                    var damage = (weapon != null) ? weapon.Damage : FistDamage;
                    targetEntity.TakeDamage(this, damage, DamageType.Physical);
                }
            }
        }

        [Command]
        public void CmdRangedAttack()
        {
            if (IsDead) return;
            if (weapon == null || weapon.Projectile == null) return;

            var projectile = Instantiate<Projectile>(weapon.Projectile, transform.position + ProjectileSpawnOffsetY * Vector3.up, Quaternion.identity);
            projectile.attacker = this;
            projectile.moveVector = new Vector3(
                (Mathf.Abs(FacingVector.x) > 0.5f) ? Mathf.Sign(FacingVector.x) : 0,
                (Mathf.Abs(FacingVector.y) > 0.5f) ? Mathf.Sign(FacingVector.y) : 0,
                 0);
            NetworkServer.Spawn(projectile.gameObject);
        }

        protected override void RpcOnTookDamage(int damage, DamageType damageType)
        {
            base.RpcOnTookDamage(damage, damageType);
            if (gameplayClient.isLocalPlayer && gameplayClient.controllerPlayers.Count == 1)
            {
                ProCamera2DShake.Instance.Shake(0);
            }
        }

        protected override void RpcDied()
        {
            base.RpcDied();
            hud.SetGameplayElementsVisible(false);
            Invoke(nameof(OnCorpseDecomposed), 1);
        }

        [ClientRpc]
        public void RpcSetAnimatorTrigger(string trigger)
        {
            Animator.SetTrigger(trigger);
        }

        private void OnCorpseDecomposed()
        {
            if (gameplayClient.isLocalPlayer)
            {
                foreach (var item in inventory.Items)
                {
                    ServerSpawnPickup(item.name, transform.position + 0.5f * new Vector3(Random.value, Random.value, 0));
                }

                ProCamera2D.Instance.RemoveCameraTarget(transform);
                gameplayClient.controllerPlayers[controllerPlayerIndex].StartCharacterCreation();
            }
        }

    }
}
