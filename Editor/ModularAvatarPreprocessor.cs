#if MODULAR_AVATAR 
using nadena.dev.ndmf;
using SEECHAK.SDK.Editor.Core;
using UnityEngine;

namespace SEECHAK.SDK.Editor
{
    public class ModularAvatarPreprocessor : IAvatarPreprocessor
    {
        public int Priority => 0;

        public void Preprocess(GameObject gameObject)
        {
            AvatarProcessor.ProcessAvatar(gameObject);
        }
    }
}

#endif