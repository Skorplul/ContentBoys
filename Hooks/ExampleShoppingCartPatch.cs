using System;
using Photon.Pun;
using UnityEngine;

namespace ContentBoys.Patches
{
    public class ExampleShoppingCartPatch
    {
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
        }

        private static void ShoppingCart_AddItemToCart(On.ShoppingCart.orig_AddItemToCart orig, ShoppingCart self, ShopItem item)
        {
            // Call the Trampoline for the Original method or another method in the Detour Chain if any exist
            orig(self, item);

            /*
             * Adding a random value to the visible price of the shopping cart typically is slightly
             * complicated due to the private setter of the CartValue property. However, as we have publicized the
             * game assembly, we do not have to worry about it, since it now is public.
             */
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
                    self.player.data.currentStamina = 100;
                }
                else
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
    }
}
