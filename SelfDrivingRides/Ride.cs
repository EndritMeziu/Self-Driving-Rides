using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SelfDrivingRides
{
    class Ride : ICloneable
    {
        private int rideId;
        private int startx;
        private int starty;
        private int endx;
        private int endy;
        public int earliestStart;
        public int latestFinish;
        public int canBeExplored;
        public Ride()
        {

        }

        public int getRideDistance()
        {
            return Math.Abs(endx - startx) + Math.Abs(endy - starty);
        }

        #region gettersAndsetters

        public int getRideId()
        {
            return this.rideId;
        }
        public void setRideId(int rideId)
        {
            this.rideId = rideId;
        }
        public int getEarliestStart()
        {
            return this.earliestStart;
        }

        public void setEarliestStart(int earliestStart)
        {
            this.earliestStart = earliestStart;
        }

        public int getLatestFinish()
        {
            return this.latestFinish;
        }

        public void setLatestFinish(int latestFinish)
        {
            this.latestFinish = latestFinish;
        }

        public int getStartX()
        {
            return this.startx;
        }
        public void setStartx(int startx)
        {
            this.startx = startx;
        }

        public int getStartY()
        {
            return this.starty;
        }
        public void setStarty(int starty)
        {
            this.starty = starty;
        }

        public int getEndX()
        {
            return this.endx;
        }
        public void setEndx(int endx)
        {
            this.endx = endx;
        }

        public int getEndY()
        {
            return this.endy;
        }
        public void setEndy(int endy)
        {
            this.endy = endy;
        }

        public int distanceToRide(int carX,int carY,int rideStartX,int rideStartY)
        {
            return Math.Abs(rideStartX - carX) + Math.Abs(rideStartY - carY);
        }

        public object Clone()
        {
            throw new NotImplementedException();
        }
        #endregion

    }
}
