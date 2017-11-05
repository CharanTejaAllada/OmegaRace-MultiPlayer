using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Net;
using CollisionManager;
using System.Diagnostics;

namespace OmegaRace
{


    class OutQueue
    {
        public static void add(Object pObj, PlayerID pID)
        {
            qHeader phead;
            Message msg = (Message)pObj;


            phead.packetOwner = pID;

            phead.type = msg.getQueuetype();
            phead.outseq = OutputQueue.seqNumGlobal;
            phead.inseq = -1;
            phead.obj = pObj;
            //
            //
            //
            OutputQueue.outQ.Enqueue(phead);
            OutputQueue.seqNumGlobal++;
        }

        //---------------------------------------------------

        public static void add(QueueType qType, Object pObj, PlayerID p)
        {
            qHeader qH;
            qH.packetOwner = p;
            qH.type = qType;
            //
            qH.outseq = OutputQueue.seqNumGlobal;
            //
            qH.inseq = -1;
            qH.obj = pObj;
            OutputQueue.outQ.Enqueue(qH);
            OutputQueue.seqNumGlobal++;
        }
    }

    //-----------------------------------------------------------------
    class OutputQueue
    {

        static public System.Collections.Generic.Queue<qHeader> outQ = new System.Collections.Generic.Queue<qHeader>();
        static public int seqNumGlobal = 9000;
        public void pushToNetwork(LocalNetworkGamer localGamer)
        {
            int icount = outQ.Count;
            for (int i = 0; i < icount; i++)
            {
                qHeader pHead = outQ.Dequeue();

                switch (pHead.type)
                {
                    case QueueType.ship_rot_anti:
                        Send(localGamer, pHead);
                        if (pHead.inseq > 1 || localGamer.IsHost)
                            inQueue.add(pHead.obj, QueueType.ship_rot_anti, pHead.outseq, pHead.packetOwner);

                        break;

                    case QueueType.ship_rot_clock:
                        Send(localGamer, pHead);
                        if (pHead.inseq > 1 || localGamer.IsHost)
                            inQueue.add(pHead.obj, QueueType.ship_rot_clock, pHead.outseq, pHead.packetOwner);

                        break;
                    case QueueType.ship_bomb:
                        Send(localGamer, pHead);
                        if (pHead.inseq > 1 || localGamer.IsHost)
                            inQueue.add(pHead.obj, QueueType.ship_bomb, pHead.outseq, pHead.packetOwner);

                        break;
                    case QueueType.ship_missile:
                        Send(localGamer, pHead);
                        if (pHead.inseq > 1 || localGamer.IsHost)
                            inQueue.add(pHead.obj, QueueType.ship_missile, pHead.outseq, pHead.packetOwner);

                        break;
                    case QueueType.ship_impulse:
                        Send(localGamer, pHead);
                        if (pHead.inseq > 1 || localGamer.IsHost)
                            inQueue.add(pHead.obj, QueueType.ship_impulse, pHead.outseq, pHead.packetOwner);

                        break;


                    case QueueType.EventMessage:
                        Send(localGamer, pHead);
                        if (pHead.inseq > 1 || localGamer.IsHost)
                            inQueue.add(pHead.obj, QueueType.EventMessage, pHead.outseq, pHead.packetOwner);

                        break;
                }
            }
        }


        public void Send(LocalNetworkGamer local, qHeader pHeader)
        {
            PacketWriter pWrite = new PacketWriter();
            pWrite.Write((int)pHeader.type);
            pWrite.Write((int)pHeader.packetOwner);
            pWrite.Write(pHeader.inseq);
            pWrite.Write(pHeader.outseq);


            switch (pHeader.type)
            {
              case QueueType.ship_rot_clock:
                                            Ship_rot_message p1 = (Ship_rot_message)pHeader.obj;
                                            pWrite.Write((int)p1.rot);
                                            pWrite.Write((int)p1.x);
                                            pWrite.Write((int)p1.y);
                                            pWrite.Write((int)p1.serverRotvalue);
                                            break;
                case QueueType.ship_rot_anti:
                                            Ship_rot_message p4 = (Ship_rot_message)pHeader.obj;
                                            pWrite.Write((int)p4.rot);
                                            pWrite.Write((int)p4.x);
                                            pWrite.Write((int)p4.y);
                                            pWrite.Write((int)p4.serverRotvalue);
                                            break;
                case QueueType.ship_missile:
                                            Ship_Create_Missile_Message missile = (Ship_Create_Missile_Message)pHeader.obj;
                                            pWrite.Write((int)missile.x);
                                            pWrite.Write((int)missile.y);
                                            pWrite.Write((int)missile.rot);
                                            break;
                case QueueType.ship_impulse:
                                            ship_impulse p = (ship_impulse)pHeader.obj;
                                            pWrite.Write((int)p.impulse.X);
                                            pWrite.Write((int)p.impulse.Y);
                                            pWrite.Write((int)p.x);
                                            pWrite.Write((int)p.y);
                                            pWrite.Write((int)p.rot);
                                            break;
                case QueueType.ship_bomb:
                                            Ship_Create_Bomb_Message bomb = (Ship_Create_Bomb_Message)pHeader.obj;
                                            pWrite.Write((int)bomb.x);
                                            pWrite.Write((int)bomb.y);
                                            pWrite.Write((int)bomb.rot);
                                            break;

                case QueueType.EventMessage:
                                            EvenMessage e = new EvenMessage((EvenMessage)pHeader.obj);
                                            pWrite.Write(e.gameIdA);
                                            pWrite.Write(e.gameIdB);
                                            pWrite.Write(e.CollisionPt);
                                            break;
            }

            local.SendData(pWrite, SendDataOptions.InOrder);
        }


    }


}
