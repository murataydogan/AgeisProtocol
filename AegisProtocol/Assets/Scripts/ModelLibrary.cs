using UnityEngine;
using System.Collections.Generic;

// 3D model yukleyici. Modelleri Assets/Resources/Models/ klasorunden, asagidaki adlarla arar:
//   turret_machinegun, turret_slow, turret_laser   (kuleler)
//   enemy_swarmer, enemy_stalker, enemy_brute      (dusmanlar)
//   engineer                                        (muhendis)
//   core                                            (enerji cekirdegi)
//   rock                                            (buz kayasi, her kaya hucresinin ustune konur)
// Model bulunamazsa nesne eski placeholder (kure/silindir) olarak kalir; hicbir sey bozulmaz.
//
// Modelin orijinal olcusu onemli degil: yukseklik asagidaki tablodaki "height" degerine otomatik olceklenir
// ve tabani yere oturtulur. Model yan/ters bakiyorsa "yaw" degerini 90 / 180 / 270 yap.
public static class ModelLibrary
{
    struct Cfg
    {
        public float height;   // dunya biriminde istenen yukseklik
        public float baseY;    // modelin tabani, ev sahibi nesnenin merkezine gore y farki
        public float yaw;      // Y ekseninde donus duzeltmesi (derece)
        public bool hideHost;  // placeholder gorselini gizle (collider kalir)

        public Cfg(float height, float baseY, float yaw, bool hideHost)
        { this.height = height; this.baseY = baseY; this.yaw = yaw; this.hideHost = hideHost; }
    }

    static readonly Dictionary<string, Cfg> Table = new Dictionary<string, Cfg>
    {
        { "turret_machinegun", new Cfg(0.9f, -0.4f, 0f, true) },
        { "turret_slow",       new Cfg(0.9f, -0.4f, 0f, true) },
        { "turret_laser",      new Cfg(1.1f, -0.4f, 0f, true) },
        { "enemy_swarmer",     new Cfg(0.5f,  0.0f, 0f, true) },
        { "enemy_stalker",     new Cfg(0.7f,  0.0f, 0f, true) },
        { "enemy_brute",       new Cfg(1.2f,  0.0f, 0f, true) },
        { "engineer",          new Cfg(0.9f, -0.3f, 0f, true) },
        { "core",              new Cfg(2.0f, -1.2f, 0f, true) },
        { "rock",              new Cfg(0.6f,  0.35f, 0f, false) },
    };

    static readonly Dictionary<string, GameObject> cache = new Dictionary<string, GameObject>();

    // Model varsa host'un cocugu olarak ekler ve ekledigi nesneyi dondurur; yoksa null.
    public static GameObject Attach(GameObject host, string modelName)
    {
        if (host == null) return null;

        Cfg cfg;
        if (!Table.TryGetValue(modelName, out cfg)) return null;

        GameObject prefab;
        if (!cache.TryGetValue(modelName, out prefab))
        {
            prefab = Resources.Load<GameObject>("Models/" + modelName);
            cache[modelName] = prefab; // yoksa null da onbellege girer
        }
        if (prefab == null) return null;

        Transform ht = host.transform;

        // Host'un olcegini (x ve z esit varsayilir) iptal eden bos bir kok: model dunya olceginde bozulmadan durur
        GameObject root = new GameObject("Visual");
        root.transform.SetParent(ht, false);
        Vector3 hs = ht.lossyScale;
        root.transform.localScale = new Vector3(1f / hs.x, 1f / hs.y, 1f / hs.z);

        GameObject model = Object.Instantiate(prefab, root.transform);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.Euler(0f, cfg.yaw, 0f);

        // Modelin collider'lari oyunu bozmasin (tiklama, OverlapSphere)
        foreach (Collider c in model.GetComponentsInChildren<Collider>()) Object.Destroy(c);

        Renderer[] rends = model.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0)
        {
            Object.Destroy(root);
            return null;
        }

        Bounds b = Combined(rends);
        if (b.size.y > 0.0001f)
            model.transform.localScale *= cfg.height / b.size.y;

        // Tabani yere, merkezi host'un uzerine oturt
        b = Combined(rends);
        Vector3 target = new Vector3(ht.position.x, ht.position.y + cfg.baseY, ht.position.z);
        model.transform.position += new Vector3(target.x - b.center.x, target.y - b.min.y, target.z - b.center.z);

        if (cfg.hideHost)
        {
            Renderer hr = host.GetComponent<Renderer>();
            if (hr != null) hr.enabled = false;
        }

        return root;
    }

    static Bounds Combined(Renderer[] rends)
    {
        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        return b;
    }
}