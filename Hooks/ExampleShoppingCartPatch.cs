using System;
using ExitGames.Client.Photon.StructWrapping;
using Photon.Pun;
using UnityEngine;

namespace ContentBoys.Patches
{
    public class ExampleShoppingCartPatch
    {
        private static bool isFreeCamActive = false; // Tracks if free camera is active
        /// <summary>
        /// Normal speed of camera movement.
        /// </summary>
        public static float normMovementSpeed = 10f;

        /// <summary>
        /// Speed of camera movement when shift is held down,
        /// </summary>
        public static float fastMovementSpeed = 100f;

        /// <summary>
        /// Sensitivity for free look.
        /// </summary>
        public static float freeLookSensitivity = 3f;

        /// <summary>
        /// Amount to zoom the camera when using the mouse wheel.
        /// </summary>
        public static float zoomSensitivity = 10f;

        /// <summary>
        /// Amount to zoom the camera when using the mouse wheel (fast mode).
        /// </summary>
        public static float fastZoomSensitivity = 50f;

        public static bool freeChanged = false;

        public static int changeCnt = 0;

        internal static void Init()
        {
            /*
             *  Subscribe with 'On.Namespace.Type.Method += CustomMethod;' for each method you're patching.
             *  Or if you are writing an ILHook, use 'IL.' instead of 'On.'
             *  Note that not all types are in a namespace, especially in Unity games.
             */

            On.ShoppingCart.AddItemToCart += ShoppingCart_AddItemToCart;
            On.ShopHandler.OnAddToCartItemClicked += ShopHandler_OnAddToCartClicked;
            On.PlayerController.Update += PlayerController_Update;
            On.PlayerController.FixedUpdate += PlayerController_FixedUpdate;
        }

        private static void ShoppingCart_AddItemToCart(On.ShoppingCart.orig_AddItemToCart orig, ShoppingCart self, ShopItem item)
        {
            // Call the Trampoline for the Original method or another method in the Detour Chain if any exist
            orig(self, item);
            self.CartValue = 0;
        }

        private static void ShopHandler_OnAddToCartClicked(On.ShopHandler.orig_OnAddToCartItemClicked orig, ShopHandler self, byte itemID)
        {
            Debug.Log("Item Clicked!");
            ShopItem shopItem = default(ShopItem);
            if (self.TryGetShopItem(itemID, ref shopItem))
            {
                int cost = self.m_ShoppingCart.CartValue + shopItem.Price;
                if (!self.m_RoomStats.CanAfford(cost) && !Configs.shopIsFree)
                {
                    self.noMoneySFX.Play(self.transform.position, false, 1f, null);
                    Debug.Log("Cant afford, dont add to cart");
                    return;
                }
                else if (PhotonNetwork.IsMasterClient)
                {
                    Debug.Log("Cant afford, adding anyway");
                }
                else if (!PhotonNetwork.IsMasterClient)
                {
                    self.noMoneySFX.Play(self.transform.position, false, 1f, null);
                    Debug.Log("Not host, cant add to cart");
                    return;
                }
            }
            if (PhotonNetwork.IsMasterClient)
            {
                self.m_PhotonView.RPC("RPCA_AddItemToCart", RpcTarget.All, new object[]
                {
                    itemID
                });
                self.addSFX.Play(self.transform.position, false, 1f, null);
                return;
            }
            self.addSFX.Play(self.transform.position, false, 1f, null);
            self.m_PhotonView.RPC("RPCM_RequestShopAction", RpcTarget.MasterClient, new object[]
            {
                2,
                itemID
            });
        }

        private static void PlayerController_Update(On.PlayerController.orig_Update orig, PlayerController self)
        {
            if (self.player.data.dead)
            {
                return;
            }
            if (self.player.NoControl())
            {
                return;
            }
            if (self.player.data.isSprinting)
            {
                if (Configs.infinitSprint)
                {
                    self.player.data.currentStamina = 10f;
                }
                else if (!Configs.infinitSprint) 
                {
                    self.player.data.currentStamina = Mathf.MoveTowards(self.player.data.currentStamina, 0f, Time.deltaTime);
                }

                if (self.player.data.currentStamina < 0.01f)
                {
                    self.player.data.staminaDepleated = true;
                }
            }
            else
            {
                if (self.player.data.sinceSprint > 1f)
                {
                    self.player.data.currentStamina = Mathf.MoveTowards(self.player.data.currentStamina, self.maxStamina, Time.deltaTime);
                }
                if (self.player.data.currentStamina >= self.staminaReActivationThreshold * 0.99f)
                {
                    self.player.data.staminaDepleated = false;
                }
            }
            if (self.player.data.isLocal)
            {
                self.Look();
                self.MovementStateChanges();
                if (self.player.input.jumpWasPressed)
                {
                    self.TryJump();
                }
            }
            self.SetRotations();
        }

        private static void PlayerController_FixedUpdate(On.PlayerController.orig_FixedUpdate orig, PlayerController self)
        {
            if (Input.GetKeyDown(Configs.freeCamButton))
            {
                isFreeCamActive = !isFreeCamActive;

                //if (isFreeCamActive && !freeChanged)
                //{
                //    isFreeCamActive = false;
                //    freeChanged = true;

                //}
                //else if (!isFreeCamActive && !freeChanged)
                //{
                //    isFreeCamActive = true;
                //    freeChanged = true;
                //}
            }
            
            //if (freeChanged && changeCnt <= 300)
            //{
            //    changeCnt++;
            //}
            //else if (freeChanged && changeCnt >= 300)
            //{
            //    changeCnt = 0;
            //    freeChanged = false;
            //}

            // Handle free camera movement
            if (isFreeCamActive)
            {
                self.ragdoll.AddForce(-self.player.data.gravityDirection * self.constantGravity, ForceMode.Acceleration);

                var fastMode = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                var movementSpeed = fastMode ? fastMovementSpeed : normMovementSpeed;

                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                {
                    self.transform.position = self.transform.position + (-self.transform.right * movementSpeed * Time.deltaTime);
                }

                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                {
                    self.transform.position = self.transform.position + (self.transform.right * movementSpeed * Time.deltaTime);
                }

                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
                {
                    self.transform.position = self.transform.position + (self.transform.forward * movementSpeed * Time.deltaTime);
                }

                if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                {
                    self.transform.position = self.transform.position + (-self.transform.forward * movementSpeed * Time.deltaTime);
                }

                if (Input.GetKey(KeyCode.Q))
                {
                    self.transform.position = self.transform.position + (self.transform.up * movementSpeed * Time.deltaTime);
                }

                if (Input.GetKey(KeyCode.E))
                {
                    self.transform.position = self.transform.position + (-self.transform.up * movementSpeed * Time.deltaTime);
                }

                if (Input.GetKey(KeyCode.R) || Input.GetKey(KeyCode.PageUp))
                {
                    self.transform.position = self.transform.position + (Vector3.up * movementSpeed * Time.deltaTime);
                }

                if (Input.GetKey(KeyCode.F) || Input.GetKey(KeyCode.PageDown))
                {
                    self.transform.position = self.transform.position + (-Vector3.up * movementSpeed * Time.deltaTime);
                }

            }
            else
            {
                orig(self);
            }
        }
    }
}
