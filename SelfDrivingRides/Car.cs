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

        public void AddRoute(Ride ride)
        {
            int startStep = this.currentDistance;

            // Add this ride to our cars list of rides
            rides.Add(ride.getRideId());

            // Calc distance to customer
            int differenceToCustX = Math.Abs(ride.getStartX() - this.getPositionX());
            int differenceToCustY = Math.Abs(ride.getStartY() - this.getPositionY());

            // Figure out what step we're on now
            currentDistance = (differenceToCustX + differenceToCustY) + currentDistance;

            // Figure out how far we've gone
            int distToCust = (differenceToCustX + differenceToCustY);

            // See if we had to wait for the customer
            int waitTime = 0;
            if (currentDistance < ride.earliestStart)
            {
                waitTime = ride.earliestStart - currentDistance;
                currentDistance = ride.earliestStart;
            }

            // Set our position to where the customer is
            this.setPositionX(ride.getStartX());
            this.setPositionY(ride.getStartY());
            
            // Figure out the distance between us and the finish position
            int differenceToDestX = Math.Abs(ride.getEndX() - this.getPositionX());
            int differenceToDestY = Math.Abs(ride.getEndY() - this.getPositionY());

            // Set our location to the finish position
            this.setPositionX(ride.getEndX());
            this.setPositionY(ride.getEndY());



            // Figure out how far we went between customer and destination
            int distToEnd = (differenceToDestX + differenceToDestY);

            // Figure out the total distance travelled
            int total = distToCust + waitTime + distToEnd;

            // Log the step we finished on
            currentDistance = total + startStep;
        }

        public RideQuality quality(Ride ride)
        {
            int posX = this.positionX;
            int posY = this.positionY;
            int thiscurrentStep = this.currentDistance;
            
            int distToRide = this.distanceToRide(ride.getStartX(), ride.getStartY());
            int thisCost = thiscurrentStep + distToRide;

            thiscurrentStep = thisCost;

            if (thiscurrentStep < ride.earliestStart)
                thiscurrentStep = ride.earliestStart;

            posX = ride.getStartX();
            posY = ride.getStartY();

            int rideLength = ride.getRideDistance();


            int total = distToRide + (thiscurrentStep - thisCost) + rideLength;

            int finishVal = total + currentDistance;

            return new RideQuality
            {
                pickUpStep = thiscurrentStep,
                carId = this.carId,
                noSteps = total,
                finishStep = finishVal
            };
        }
       
        #endregion
    }
}
