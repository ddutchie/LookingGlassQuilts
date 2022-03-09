using UnityEngine;
using UnityEngine.Video;
using System.Reflection;
public static class VideoPlayerExtensions
{
    public static VideoPlayer MakeCopy(this VideoPlayer original, bool copyURL=false)
    {
        var copy = original.gameObject.AddComponent<VideoPlayer>();
        PropertyInfo[] p = original.GetType().GetProperties();
 
        foreach (PropertyInfo prop in p)
        {
            if (!copyURL && prop.Name.Equals("url"))
                continue;
            try
            {
                prop.SetValue(copy, prop.GetValue(original));
            }
            catch
            {}
        }
   
        GameObject.Destroy(original);
 
        return copy;
    }
}