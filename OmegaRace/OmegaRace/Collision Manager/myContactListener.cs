using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Box2D.XNA;
using CollisionManager;
using Microsoft.Xna.Framework;

namespace OmegaRace
{
    class myContactListener : IContactListener
    {



        public  void BeginContact(Contact contact)
        {
            GameObject A = (GameObject)contact.GetFixtureA().GetUserData();
            GameObject B = (GameObject)contact.GetFixtureB().GetUserData();


            Manifold manifold;
            Transform xfA;
            Transform xfB;
            float radiusA;
            float radiusB;

            contact.GetFixtureA().GetBody().GetTransform(out xfA);
            contact.GetFixtureB().GetBody().GetTransform(out xfB);
            radiusA = contact.GetFixtureA().GetShape()._radius;
            radiusB = contact.GetFixtureB().GetShape()._radius;

            contact.GetManifold(out manifold);

            WorldManifold worldManifold = new WorldManifold(ref manifold,ref xfA,radiusA,ref xfB,radiusB);

            Vector2 ptA = worldManifold._points[0];
            Vector2 ptB = worldManifold._points[1];

            //System.Console.Write(" point {0} {1}\n", ptA, ptB);
            
            EvenMessage e=new EvenMessage(A.gameId,B.gameId,ptA);
            

            if (A.type == GameObjType.p1Bomb || A.type == GameObjType.p1missiles || A.type == GameObjType.p1ship)
                OutQueue.add(e, PlayerID.one);
            else if (A.type == GameObjType.p2Bomb || A.type == GameObjType.p2missiles || A.type == GameObjType.p2ship)
                OutQueue.add(e, PlayerID.two);
            else if (B.type == GameObjType.p1Bomb || B.type == GameObjType.p1missiles || B.type == GameObjType.p1ship)
                OutQueue.add(e, PlayerID.one);
            else if (B.type == GameObjType.p2Bomb || B.type == GameObjType.p2missiles || B.type == GameObjType.p2ship)
                OutQueue.add(e, PlayerID.two);
            else
                OutQueue.add(e, PlayerID.one);
        }

        public  void EndContact(Contact contact)
        {
            
        }

        public void PostSolve(Contact contact, ref ContactImpulse impulse)
        {
        }

        public void PreSolve(Contact contact, ref Manifold oldManifold)
        {
        }

        


    }
}
