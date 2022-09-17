using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SmallyGames.Shop;

namespace FidgetSweeper
{
    [CreateAssetMenu(fileName = "Resources Manager", menuName = "SmallyGames/Fidget Pop/Resouces Manager")]
    public class ResourcesManager : ScriptableObject
    {
        [SerializeField]
        private Category level;
        public Category Level => level;
    }
}
