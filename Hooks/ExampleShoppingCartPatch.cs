using System;
using Photon.Pun;

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
            ShopItem shopItem = default(ShopItem);
            if (self.TryGetShopItem(itemID, ref shopItem))
            {
                int cost = self.m_ShoppingCart.CartValue + shopItem.Price;
                if (!self.m_RoomStats.CanAfford(cost))
                {
                    ContentBoys.Logger.LogDebug("not enough money");
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
            ContentBoys.Logger.LogDebug("Added anyways");
        }
    }
}
