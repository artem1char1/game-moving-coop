using NUnit.Framework;
using MovingCoop.Application;
using MovingCoop.Domain;
using System.Collections.Generic;
namespace MovingCoop.Tests {
 public sealed class AuthorityRulesTests {
 sealed class World:ICargoWorld {public GroundPoint Move(GroundPoint a,GroundPoint d)=>new(a.X+d.X,a.Z+d.Z);public bool Clear(GroundPoint a,GroundPoint b)=>true;public bool CargoFits(CargoState c,GroundPoint a,float angle)=>true;}
 static void Command(AuthoritySession a,string kind,int seq,int? shift=null){a.Enqueue(0,new ClientCommand{session=a.State.SessionId,shift=shift??a.State.ShiftId,kind=kind,sequence=seq});}
 static AuthoritySession Active(){var a=new AuthoritySession(new World());a.Connect(0,true);a.Connect(1,false);a.State.MarkReady(1);Command(a,"start",1);a.Tick(0,.02f);return a;}
 [Test]public void DeliveryLedgerIsIdempotent(){var c=new CargoState(1,new GroundPoint(0,-6));var ledger=new HashSet<int>();Assert.IsTrue(c.FullyInside());Assert.IsTrue(c.Deliver(ledger));Assert.IsFalse(c.Deliver(ledger));Assert.AreEqual(1,ledger.Count);}
 [Test]public void PartialBoundsFail(){var c=new CargoState(10,new GroundPoint(1.5f,-6));Assert.IsFalse(c.FullyInside());}
 [Test]public void HandleRaceHasOneWinner(){var c=new CargoState(10,new GroundPoint(0,0));Assert.IsTrue(c.Grab(0,0));Assert.IsFalse(c.Grab(0,1));Assert.IsTrue(c.Grab(1,1));c.Release();Assert.AreEqual(0,c.Carriers.Count);}
 [Test]public void DeadlineBeforeQueuedEvacuation(){var a=Active();Command(a,"evacuate",2);a.Tick(300,.02f);Assert.AreEqual("Failure: Deadline",a.Snapshot().result);}
 [Test]
 public void DeliveredQuotaSucceedsAtDeadlineWithoutEvacuation()
 {
     // Drive the real application using intents against a collision-free world.
     // No ledger, total, cargo pose or result is assigned by the fixture.
     var authority = Active();
     double now = 0;
     int inputSequence = 0, operationSequence = 1;
     void Step(float dt, string action = null, float x = 0, float z = 0)
     {
         authority.Enqueue(0, new ClientCommand {
             session = authority.State.SessionId, shift = authority.State.ShiftId,
             kind = "input", sequence = ++inputSequence, x = x, z = z
         });
         if (action != null) Command(authority, action, ++operationSequence);
         now += dt;
         authority.Tick(now, dt);
     }
     void TravelTo(float x, float z, int cargoId = 0)
     {
         for (int guard = 0; guard < 1000; guard++)
         {
             var frame = authority.Snapshot();
             float fromX = frame.players[0].x, fromZ = frame.players[0].z;
             if (cargoId != 0)
             {
                 var cargo = System.Array.Find(frame.cargo, c => c.id == cargoId);
                 Assert.AreEqual(0, cargo.first, "Host must remain the carrier during delivery.");
                 fromX = cargo.x; fromZ = cargo.z;
             }
             float dx = x - fromX, dz = z - fromZ;
             float distance = (float)System.Math.Sqrt(dx * dx + dz * dz);
             if (distance < .001f) return;
             float speed = cargoId == 0 ? 4 : 3;
             Step(System.Math.Min(.05f, distance / speed), x: dx / distance, z: dz / distance);
         }
         Assert.Fail("Application did not reach the intended position.");
     }
     for (int id = 1; id <= 8; id++)
     {
         var box = System.Array.Find(authority.Snapshot().cargo, c => c.id == id);
         TravelTo(box.x - .65f, box.z);
         Step(.02f, "interact");
         TravelTo(0, -6, id);
         Step(.02f, "interact");
         Step(.51f);
         Assert.IsTrue(System.Array.Find(authority.Snapshot().cargo, c => c.id == id).delivered);
         Assert.AreEqual(id * 40, authority.Snapshot().total);
     }
     Assert.AreEqual(ShiftPhase.Active, authority.State.Phase, "Quota alone must not end the shift.");
     TravelTo(-8, 8); // Deadline success does not require movers to be in the van zone.
     Assert.Less(now, 300);
     authority.Tick(300, .02f);
     var result = authority.Snapshot();
     Assert.AreEqual(ShiftPhase.Finished, authority.State.Phase);
     Assert.AreEqual("Success: Deadline", result.result);
     Assert.AreEqual(320, result.total);
     Assert.AreEqual(0, result.remaining);
     authority.Tick(301, .02f);
     Assert.AreEqual("Success: Deadline", authority.Snapshot().result);
     Assert.AreEqual(320, authority.Snapshot().total);
 }
 [Test]public void TwoRestartsRejectOldShift(){var a=Active();for(int i=0;i<2;i++){int old=a.State.ShiftId;a.Tick(i*400+300,.02f);Command(a,"restart",200+i);a.Tick(i*400+301,.02f);Assert.AreEqual(old+1,a.State.ShiftId);Command(a,"start",100,old);a.Tick(i*400+302,.02f);Assert.AreEqual(ShiftPhase.Preparing,a.State.Phase);a.State.MarkReady(1);Command(a,"start",20+i);a.Tick(i*400+303,.02f);}}
 static SessionState Pair(){var s=new SessionState();s.Add(0);s.Add(1);s.MarkReady(0);s.MarkReady(1);s.Start();return s;}
 [Test]public void SofaRequiresTwoAndAveragesInputs(){var sim=new CargoSimulation(new World());sim.Items.Clear();var c=new CargoState(10,new GroundPoint(0,0));sim.Items.Add(c);var s=Pair();c.Grab(0,0);sim.Step(s,1,1,id=>(1,0,0));Assert.AreEqual(0,c.Position.X);c.Grab(1,1);sim.Step(s,1,2,id=>id==0?(1,0,0):(0,0,0));Assert.AreEqual(1,c.Position.X);sim.Step(s,1,3,id=>id==0?(1,0,0):(-1,0,0));Assert.AreEqual(1,c.Position.X);sim.Step(s,1,4,id=>(1,0,0));Assert.AreEqual(3,c.Position.X);}
 [Test]public void OnlyLeaderRotatesSofa(){var sim=new CargoSimulation(new World());sim.Items.Clear();var c=new CargoState(10,new GroundPoint(0,0));sim.Items.Add(c);var s=Pair();c.Grab(0,1);c.Grab(1,0);sim.Step(s,1,1,id=>id==0?(0,0,1):(0,0,0));Assert.AreEqual(0,c.Angle);sim.Step(s,1,2,id=>id==1?(0,0,1):(0,0,0));Assert.AreEqual(90,c.Angle);}
 [Test]public void FullBoundsMustDwellForHalfSecond(){var sim=new CargoSimulation(new World());sim.Items.Clear();var c=new CargoState(1,new GroundPoint(0,-6));sim.Items.Add(c);var s=Pair();sim.Step(s,.02f,1,id=>(0,0,0));sim.Step(s,.02f,1.49,id=>(0,0,0));Assert.AreEqual(0,sim.Total);sim.Step(s,.02f,1.5,id=>(0,0,0));Assert.AreEqual(40,sim.Total);sim.Step(s,.02f,2,id=>(0,0,0));Assert.AreEqual(40,sim.Total);}
 [Test]public void DuplicateEvacuationAndFinishedInputCannotMutateResult(){var a=Active();Command(a,"evacuate",2);Command(a,"evacuate",2);a.Tick(1,.02f);Assert.AreEqual(ShiftPhase.Finished,a.State.Phase);var before=a.State.Find(0).Position;a.Enqueue(0,new ClientCommand{session=a.State.SessionId,shift=1,kind="input",sequence=1,x=1});a.Tick(2,1);Assert.AreEqual(before.X,a.State.Find(0).Position.X);Assert.AreEqual(0,a.Snapshot().total);}
 [Test]public void FirstApproachReservesHandleDespiteSecondPlayerBeingCloser(){var sim=new CargoSimulation(new World());sim.Items.Clear();var c=new CargoState(1,new GroundPoint(0,0));sim.Items.Add(c);var state=Pair();state.Move(0,new GroundPoint(-1.75f,0));state.Move(1,new GroundPoint(-.75f,0));sim.Interact(state,0);Assert.IsTrue(sim.Approaching(0));Assert.AreEqual(0,c.Carriers.Count);sim.Interact(state,1);Assert.AreEqual("Occupied",sim.Feedback);Assert.IsFalse(sim.Approaching(1));for(int i=0;i<20;i++)sim.Step(state,.02f,i*.02,id=>(0,0,0));Assert.AreEqual(0UL,c.Carriers[0]);}
 [Test]public void CancelledReservationCanBeAcquiredByNextPlayer(){var sim=new CargoSimulation(new World());sim.Items.Clear();var c=new CargoState(1,new GroundPoint(0,0));sim.Items.Add(c);var state=Pair();state.Move(0,new GroundPoint(-1.75f,0));state.Move(1,new GroundPoint(-.75f,0));sim.Interact(state,0);sim.Cancel(0);sim.Interact(state,1);Assert.IsTrue(sim.Approaching(1));for(int i=0;i<4;i++)sim.Step(state,.02f,i*.02,id=>(0,0,0));Assert.AreEqual(1UL,c.Carriers[0]);sim.Reset();Assert.IsFalse(sim.Approaching(1));}
 [Test]public void ActiveSnapshotsPreserveDisconnectNotice(){var incoming=new SessionFrame{phase=1};const string notice="Client disconnected. Host may evacuate at the van, then restart.";var text=MovingCoop.UnityAdapters.SnapshotNotice.Apply(0,incoming,"Ready");Assert.AreEqual("SHIFT ACTIVE",text);for(int i=0;i<20;i++)Assert.AreEqual(notice,MovingCoop.UnityAdapters.SnapshotNotice.Apply(1,incoming,notice));}
 [Test]public void ReleaseResetsDeliveryDwell(){var c=new CargoState(1,new GroundPoint(0,-6));c.InsideSince=1;c.Grab(0,0);Assert.AreEqual(-1,c.InsideSince);c.Release();Assert.AreEqual(0,c.Carriers.Count);}
 }
}

