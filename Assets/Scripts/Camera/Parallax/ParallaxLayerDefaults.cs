using UnityEngine;

public static class ParallaxLayerDefaults
{
    public static ParallaxLayer Create(
        Transform target)
    {
        ParallaxLayer layer =
            new ParallaxLayer
            {
                target = target,
                useVerticalParallax = true,
                snapTransformToPixelGrid = false
            };


        if (target == null)
        {
            return layer;
        }


        ApplyStrengthByName(
            layer,
            target.name
        );


        return layer;
    }


    private static void ApplyStrengthByName(
        ParallaxLayer layer,
        string objectName)
    {
        switch (objectName)
        {
            case "Plane_1_other":
            case "Plane_1":

                layer.horizontalStrength =
                    0.005f;

                layer.verticalStrength =
                    0.002f;

                break;


            case "Plane_2":

                layer.horizontalStrength =
                    0.08f;

                layer.verticalStrength =
                    0.025f;

                break;


            case "Plane_3":

                layer.horizontalStrength =
                    0.14f;

                layer.verticalStrength =
                    0.04f;

                break;


            case "Plane_4":

                layer.horizontalStrength =
                    0.22f;

                layer.verticalStrength =
                    0.06f;

                break;


            default:

                layer.horizontalStrength =
                    0.1f;

                layer.verticalStrength =
                    0.03f;

                break;
        }
    }


    public static void Clamp(
        ParallaxLayer layer)
    {
        if (layer == null)
            return;


        layer.horizontalStrength =
            Mathf.Clamp01(
                layer.horizontalStrength
            );


        layer.verticalStrength =
            Mathf.Clamp01(
                layer.verticalStrength
            );
    }
}