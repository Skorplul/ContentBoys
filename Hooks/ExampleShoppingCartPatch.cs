using System;
using Photon.Pun;
using UnityEngine;

namespace ContentBoys.Patches
{
    public class ExampleShoppingCartPatch
    {
        private static bool isFreeCamActive = false; // Tracks if free camera is active
        private static Vector3 freeCamPosition;     // Stores free camera position
        private static Quaternion freeCamRotation; // Stores free camera rotation
        private static float freeCamSpeed = 10f;    // Speed of free camera movement
        private static float freeCamLookSpeed = 2f; // Speed of camera rotation
        private static Transform? originalParent; // Stores the camera's original parent
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
            // Free camera mode toggle (e.g., by pressing "F")
            if (Input.GetKeyDown(Configs.freeCamButton))
            {
                isFreeCamActive = !isFreeCamActive;

                if (isFreeCamActive)
                {
                    // Enter free camera mode
                    originalParent = Camera.main.transform.parent;
                    Camera.main.transform.parent = null; // Remove from hierarchy
                    freeCamPosition = Camera.main.transform.position;
                    freeCamRotation = Camera.main.transform.rotation;
                }
                else
                {
                    // Exit free camera mode, restore player position/rotation
                    Camera.main.transform.parent = originalParent;
                    Camera.main.transform.localPosition = Vector3.zero; // Reset position relative to parent
                    Camera.main.transform.localRotation = Quaternion.identity; // Reset rotation relative to parent
                }
            }

            // Handle free camera movement
            if (isFreeCamActive)
            {
                // Free camera position movement
                float moveX = 0f;
                if (Input.GetKey(KeyCode.A)) moveX += freeCamSpeed * Time.deltaTime; // A
                if (Input.GetKey(KeyCode.D)) moveX -= freeCamSpeed * Time.deltaTime; // D
                float moveZ = 0f;
                if (Input.GetKey(KeyCode.W)) moveZ += freeCamSpeed * Time.deltaTime; // W
                if (Input.GetKey(KeyCode.S)) moveZ -= freeCamSpeed * Time.deltaTime; // S
                float moveY = 0f;

                if (Input.GetKey(KeyCode.E)) moveY += freeCamSpeed * Time.deltaTime; // Ascend (E)
                if (Input.GetKey(KeyCode.Q)) moveY -= freeCamSpeed * Time.deltaTime; // Descend (Q)

                freeCamPosition += Camera.main.transform.forward * moveZ +
                                   Camera.main.transform.right * moveX +
                                   Camera.main.transform.up * moveY;

                // Free camera rotation (mouse look)
                float lookX = Input.GetAxis("Mouse X") * freeCamLookSpeed;
                float lookY = -Input.GetAxis("Mouse Y") * freeCamLookSpeed;

                freeCamRotation *= Quaternion.Euler(lookY, lookX, 0f);

                // Apply position and rotation to the camera
                Camera.main.transform.position = freeCamPosition;
                Camera.main.transform.rotation = freeCamRotation;
            }

            // Original Logic
            if (self.player.Ragdoll())
            {
                return;
            }
            if (!self.player.data.physicsAreReady)
            {
                return;
            }
            if (self.player.data.simplifiedRagdoll && self.player.refs.view.IsMine)
            {
                self.SimpleMovement();
                return;
            }
            if (!self.player.data.carried)
            {
                self.ConstantGravity();
                if (!self.player.data.isGrounded)
                {
                    self.Gravity();
                }
                else
                {
                    self.Standing();
                }
            }
            if (isFreeCamActive)
                return;
            self.Movement();
            self.BodyRotation();
            if (self.jumpForceTime > 0f)
            {
                self.ApplyJumpForce();
            }
        }
    }
}
