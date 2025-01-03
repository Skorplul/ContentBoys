using System;
using Photon.Pun;
using UnityEngine;

namespace ContentBoys.Patches
{
    public class ExampleShoppingCartPatch
    {
        /// <summary>
        /// Normal speed of camera movement.
        /// </summary>
        public static float normMovementSpeed = 1f;

        /// <summary>
        /// Speed of camera movement when shift is held down,
        /// </summary>
        public static float fastMovementSpeed = 10f;

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

        private static Transform? originalParent = null; // Stores the camera's original parent

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
            if (Configs.freeCam)
            {
                // not working, don't use
                // it's supposed to decouple the cam but wellp it doesn't

                //if (Configs.freeCam)
                //{
                //    // Detach the camera
                //    originalParent = Camera.main.transform.parent;
                //    Camera.main.transform.parent = null; // Remove from hierarchy
                //    self.StopCoroutine(self.player.toggleCollisionCor);
                //}
                //else
                //{
                //    // Reattach the camera
                //    Camera.main.transform.parent = originalParent;
                //    Camera.main.transform.localPosition = Vector3.zero; // Reset position relative to parent
                //    Camera.main.transform.localRotation = Quaternion.identity; // Reset rotation relative to parent
                //    if (self.player.toggleCollisionCor != null)
                //    {
                //        Debug.Log("Cancelling current corutine and resetting colliders!");
                //        self.StopCoroutine(self.player.toggleCollisionCor);
                //        for (int i = 0; i < self.player.collidersToToggleList.Count; i++)
                //        {
                //            if (!(self.player.collidersToToggleList[i] == null))
                //            {
                //                self.player.collidersToToggleList[i].enabled = self.player.collidersEnabled[i];
                //            }
                //        }
                //        self.player.collidersEnabled.Clear();
                //        self.player.collidersToToggleList.Clear();
                //        self.player.toggleCollisionCor = null;
                //    }
                }
            }

            // Handle free camera movement
            if (Configs.freeCam)
            {
                var fastMode = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                var movementSpeed = fastMode ? fastMovementSpeed : normMovementSpeed;

                if (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.PageUp))
                {
                    self.transform.position = self.transform.position + (Vector3.up * (movementSpeed * 5) * Time.deltaTime);
                }

                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.PageDown))
                {
                    self.transform.position = self.transform.position + (-Vector3.up * (movementSpeed * 5) * Time.deltaTime);
                }

                Vector3 lookDirection = self.player.data.lookDirection;
                Vector3 lookDirectionRight = self.player.data.lookDirectionRight;
                lookDirection.y = 0f;
                lookDirection.Normalize();
                Vector3 planeNormal = self.player.data.groundNormal;
                if (self.player.data.sinceGrounded > 0.2f)
                {
                    planeNormal = -self.player.data.gravityDirection;
                }
                Vector3 a = HelperFunctions.GroundDirection(planeNormal, -lookDirectionRight);
                Vector3 a2 = HelperFunctions.GroundDirection(planeNormal, lookDirection);
                Vector3 vector = a * self.player.input.movementInput.y + a2 * self.player.input.movementInput.x;
                vector = Vector3.ClampMagnitude(vector, 1f);
                if (self.wallClimb)
                {
                    vector = self.player.data.lookDirection;
                }
                self.player.data.movementVector = vector;
                self.ragdoll.AddForce(self.player.data.movementVector * movementSpeed * self.movementForce, ForceMode.Impulse);
            }
            else
            {
                orig(self);
            }
        }
    }
}
