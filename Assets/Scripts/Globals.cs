using System;
using System.Collections.Generic;
using UnityEngine;

namespace Whatwapp {

    public enum Suit {
        QUADRI,
        CUORI,
        PICCHE,
        FIORI
    }

    public enum Color {
        ROSSO,
        NERO
    }

    public enum Value {
        ASSO = 1,
        DUE = 2,
        TRE = 3,
        QUATTRO = 4,
        CINQUE = 5,
        SEI = 6,
        SETTE = 7,
        OTTO = 8,
        NOVE = 9,
        DIECI = 10,
        JACK = 11,
        DONNA = 12,
        RE = 13
    }

    public enum DeckType {
        MAIN,
        COLUMN,
        BASE,
        DRAW
    }

    public enum Scores {
        FromColumnToBase = 15,
        FromDrawToBase = 10,
        FromDrawToColumn = 5,
        FlippedCard = 5
    }

    public static class Tables {
        public static Dictionary<Suit, Color> SuitsColors = new Dictionary<Suit, Color> {
            { Suit.QUADRI, Color.ROSSO },
            { Suit.CUORI, Color.ROSSO },
            { Suit.PICCHE, Color.NERO },
            { Suit.FIORI, Color.NERO }
        };        
    }

    public static class Utility {
        public static float OverlapArea(Rect rect1, Rect rect2) {
            float overlapX, overlapY;
            overlapX = Math.Max(0, Math.Min(rect1.xMax, rect2.xMax) - Math.Max(rect1.xMin, rect2.xMin));
            overlapY = Math.Max(0, Math.Min(rect1.yMax, rect2.yMax) - Math.Max(rect1.yMin, rect2.yMin));
            return overlapX * overlapY;
        }

        public static Rect RectOfCollider(Collider2D collider) {
            Vector2 size = collider.bounds.size;
            Vector3 worldPos = collider.transform.TransformPoint(collider.bounds.center);
            Rect rect = new Rect(0f, 0f, size.x, size.y);
            rect.center = new Vector2(worldPos.x, worldPos.y);
            return rect;
        }
    }
}