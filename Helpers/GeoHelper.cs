namespace carrentalmvc.Helpers
{
    public static class GeoHelper
    {
        /// <summary>
        /// Tính khoảng cách giữa 2 tọa độ theo công thức Haversine (đơn vị: km)
        /// </summary>
        public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // Bán kính Trái Đất (km)

            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c;
        }

        private static double ToRadians(double angle)
        {
            return Math.PI * angle / 180.0;
        }

        /// <summary>
        /// Tính phí giao xe dựa trên khoảng cách
        /// </summary>
        public static decimal CalculateDeliveryFee(double distanceKm)
        {
            const decimal BASE_FEE = 30000;
            const decimal FEE_PER_KM = 5000;
            const decimal MAX_FEE = 200000;
            const double FREE_DISTANCE = 5;

            if (distanceKm <= FREE_DISTANCE)
                return BASE_FEE;

            var extraKm = distanceKm - FREE_DISTANCE;
            var calculatedFee = BASE_FEE + ((decimal)extraKm * FEE_PER_KM);

            return Math.Min(calculatedFee, MAX_FEE);
        }
    }
}