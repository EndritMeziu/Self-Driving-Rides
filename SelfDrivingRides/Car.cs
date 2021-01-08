using System;   
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SelfDrivingRides
{
    class Car
    {
        private int carId;
        private int positionX;
        private int positionY;
        public int currentDistance;
        public List<int> rides;
        public List<int> ridesCost;
        public double carScore;
        public Car()
        {
            this.positionX = 0;
            this.positionY = 0;
            this.carScore = 0;
            this.currentDistance = 0;
            rides = new List<int>();
            ridesCost = new List<int>();
        }


        public int distanceToRide(int ridex,int ridey)
        {
            return Math.Abs(positionX - ridex) + Math.Abs(positionY - ridey);
        }

        #region gettersAndsetters

        public int getCurrentCarDistance()
        {
            return this.currentDistance;
        }

        public void setCurrentCarDistance(int currentDistance)
        {
            this.currentDistance = currentDistance;
        }
        public int getCarId()
        {
            return this.carId;
        }

        public void setCarId(int carId)
        {
            this.carId = carId;
        }

        public List<int> getRidesCost()
        {
            return this.ridesCost;
        }

        public void addRideCost(int cost)
        {
            this.ridesCost.Add(cost);
        }
        
        public List<int> getRides()
        {
            return this.rides;
        }
        
        public void addRide(int rideId)
        {
            this.rides.Add(rideId);
        }

        public void removeRide(int rideId)
        {
            this.rides.Remove(rideId);
        }
        public int getPositionX()
        {
            return this.positionX;
        }

        public void setPositionX(int positionX)
        {
            this.positionX = positionX;
        }

        public int getPositionY()
        {
            return this.positionY;
        }

        public void setPositionY(int positionY)
        {
            this.positionY = positionY;
        }

       


        public RideQuality quality(Ride ride)
        {
            int actualDist = this.currentDistance;
            int distToRide = this.distanceToRide(ride.getStartX(), ride.getStartY());
            int ridePickupTime = actualDist + distToRide;
            if (ridePickupTime < ride.earliestStart)
                ridePickupTime = ride.earliestStart;


            int totalSteps = ridePickupTime + ride.getRideDistance() - actualDist;
            int finishSttep = totalSteps + actualDist;
            return new RideQuality
            {
                pickUpStep = ridePickupTime,
                carId = this.carId,
                noSteps = totalSteps,
                finishStep = finishSttep,
                rideId = ride.getRideId()
            };
        }

        
       
        #endregion
    }
}
