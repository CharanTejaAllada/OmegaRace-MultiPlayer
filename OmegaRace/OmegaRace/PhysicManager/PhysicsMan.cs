using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Box2D.XNA;
using CollisionManager;

namespace OmegaRace
{
    class PhysicsMan : Manager
    {
        private static PhysicsMan instance;
        private static int count = 0;

        private PhysicsMan()
        {

            //......................

            //Debug.WriteLine("");

        }


        public static PhysicsMan Instance()
        {
            if (instance == null)
                instance = new PhysicsMan();
            return instance;
        }

        public void addPhysicsObj(GameObject _gameObj,Body _body)
        {
            PhysicsObj obj = new PhysicsObj(_gameObj, _body);
            _gameObj.physicsObj = obj;

            this.privActiveAddToFront((ManLink)obj, ref this.active);
        }

        public void Update()
        {
            ManLink ptr = this.active;
            PhysicsObj physNode = null;
            Body body = null;
            

            while (ptr != null)
            {
                
                physNode = (PhysicsObj)ptr;
                body = physNode.body;
                physNode.gameObj.pushPhysics(body.GetAngle(), body.Position);
                
                ptr = ptr.next;
            }
        }

        public ManLink GetNode() 
        {
            return this.active;
        }

        public static void Update(ref physics_buffer_message p)
        {
            ManLink ptr = instance.active;
            

            PhysicsObj physNode = null;
            Body body = null;
            int i = 0;

            while (ptr != null)
            {
                physNode = (PhysicsObj)ptr;
                body = physNode.body;

                if (p.buff[i].id == physNode.gameObj.gameId)
                {
                    physNode.body.Position = p.buff[i].position;
                    physNode.body.Rotation = p.buff[i].rotation;
                    physNode.gameObj.pushPhysics(body.GetAngle(), body.Position);
                }

                i++;
                ptr = ptr.next;
            }

        }


        public static int getCount()
        {
            return PhysicsMan.count;
        }

        public static void pushToBuffer(ref physics_buffer[] p)
        {
            PhysicsMan pMan = PhysicsMan.instance;

            ManLink ptr = pMan.active;
            PhysicsObj phyNode = null;
            Body body = null;

            int i = 0;
            while (ptr != null)
            {
                phyNode = (PhysicsObj)ptr;
                body = phyNode.body;

                p[i].id  = phyNode.gameObj.gameId;
                p[i].position = body.Position;
                p[i].rotation = body.GetAngle();

                i++;
                ptr = ptr.next;
            }

        }



        public void removePhysicsObj(PhysicsObj _obj)
        {
            this.privActiveRemoveNode((ManLink)_obj, ref this.active);

        }

        protected override object privGetNewObj()
        {
            throw new NotImplementedException();
        }

    }
}
