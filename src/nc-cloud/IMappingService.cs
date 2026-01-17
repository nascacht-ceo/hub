using System.Collections.Generic;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;

/// <summary>
/// Defines a contract for a service that provides location-based functionality,
/// leveraging the industry-standard NetTopologySuite (NTS) Point model for geodetic data.
/// All core methods are structured for batch processing using IEnumerable<T> inputs.
/// 
/// Note on NTS: Point.Y corresponds to Latitude, and Point.X corresponds to Longitude.
/// </summary>
public interface IMappingService
{
	/// <summary>
	/// Represents a complete, structured address, aligning with Azure Maps/RFC Civic Address concepts.
	/// Renamed from CivicAddress for improved clarity and simplicity in application code.
	/// </summary>
	public class MapAddress
	{
		public string? StreetName { get; set; }
		public string? HouseNumber { get; set; }
		public string? Municipality { get; set; } // City (vCard city)
		public string? CountrySubdivision { get; set; } // State/Province (vCard region)
		public string? PostalCode { get; set; } // Postal Code (vCard postal-code)
		public string? CountryCode { get; set; } // ISO 3166-1 Alpha-2 (vCard country)
		public bool IsDeliverable { get; set; }
	}

	/// <summary>
	/// Represents a discovered entity (e.g., a Place or POI), combining the NTS Point with the structured Address and metadata.
	/// Renamed from LocationObject to Location for improved clarity.
	/// </summary>
	public class MapLocation
	{
		/// <summary>The geodetic location represented by the NTS Point (Y=Lat, X=Long).</summary>
		public Point? GeodeticLocation { get; set; }

		public MapAddress? Address { get; set; }
		public string? Name { get; set; }
		public string? SourceIdentifier { get; set; } // Provider-specific ID (e.g., PlaceId)
		public double? Confidence { get; set; } // Confidence score/match quality
	}

	/// <summary>
	/// Represents a calculated route, utilizing IEnumerable<Point> for path data.
	/// </summary>
	public class MapRoute
	{
		public double DistanceInMeters { get; set; }
		public double DurationInSeconds { get; set; }

		/// <summary>The sequence of NTS Points defining the route geometry.</summary>
		public IEnumerable<Point> Path { get; set; } = [];
		public string? Summary { get; set; }
	}

	/// <summary>
	/// Represents a single request for route calculation for batch input.
	/// </summary>
	public class MapRouteRequest
	{
		public Point? Origin { get; set; }
		public Point? Destination { get; set; }
		public IEnumerable<Point> Waypoints { get; set; } = [];
		public string? TravelMode { get; set; }
	}

	/// <summary>
	/// Represents a single asset position for batch processing.
	/// Renamed from AssetPositionUpdate to AssetPosition.
	/// </summary>
	public class AssetPosition
	{
		public string? AssetId { get; set; }
		public Point? Position { get; set; }
		public System.DateTimeOffset? Timestamp { get; set; }
	}

	// --- Core Functionality Methods (structured for batch processing) ---

	#region Location and Address Services

	/// <summary>
	/// Converts a collection of addresses into geodetic locations (Batch Geocoding).
	/// </summary>
	/// <param name="addresses">The collection of full address strings to geocode.</param>
	/// <returns>An enumerable collection of matching Location entities for each input address.</returns>
	IAsyncEnumerable<MapLocation> GeocodeAddressesAsync(IEnumerable<string> addresses);

	/// <summary>
	/// Converts a collection of Geodetic Locations (NTS Points) to Addresses (Batch Reverse Geocoding).
	/// </summary>
	/// <param name="locations">The collection of NTS Points (Y=Lat, X=Long) to reverse geocode.</param>
	/// <returns>An enumerable collection of matching Location entities for each input location.</returns>
	IAsyncEnumerable<MapLocation> ReverseGeocodeLocationsAsync(IEnumerable<Point> locations);

	/// <summary>
	/// Validates and standardizes a collection of addresses into structured Address objects.
	/// </summary>
	/// <param name="unstructuredAddresses">The collection of unstructured address strings to validate.</param>
	/// <returns>An enumerable collection of validated Address objects.</returns>
	IAsyncEnumerable<MapAddress> ValidateAddressesAsync(IEnumerable<string> unstructuredAddresses);

	/// <summary>
	/// Searches for nearby points of interest (POI) using an NTS Point as the center (single query focus).
	/// </summary>
	/// <remarks>Kept as a single search query, as POI search is typically focused on one area/query.</remarks>
	IAsyncEnumerable<MapLocation> SearchLocationAsync(string query, Point center, int? radiusInMeters = null);

	#endregion

	// ---

	#region Routing and Tracking

	/// <summary>
	/// Calculates a collection of optimal routes based on a collection of RouteRequest objects.
	/// </summary>
	/// <param name="requests">A collection of RouteRequest objects defining origins, destinations, and modes.</param>
	/// <returns>An enumerable collection of calculated Route objects.</returns>
	IAsyncEnumerable<MapRoute> CalculateRoutesAsync(IEnumerable<MapRouteRequest> requests);

	/// <summary>
	/// Calculates a matrix of travel times and distances between multiple origins and destinations.
	/// </summary>
	/// <param name="origins">A collection of starting NTS Points.</param>
	/// <param name="destinations">A collection of ending NTS Points.</param>
	/// <returns>An enumerable collection of route rows, where each row is an enumerable collection of Route objects.</returns>
	IAsyncEnumerable<IEnumerable<MapRoute>> CalculateRouteMatrixAsync(IEnumerable<Point> origins, IEnumerable<Point> destinations);

	/// <summary>
	/// Checks a collection of Geodetic Locations (NTS Points) against defined geofences.
	/// </summary>
	/// <param name="locations">The collection of NTS Points to check.</param>
	/// <returns>An enumerable collection where each element is the list of geofence IDs the corresponding input location is inside.</returns>
	IAsyncEnumerable<IEnumerable<string>> CheckGeofencesBatchAsync(IEnumerable<Point> locations);

	/// <summary>
	/// Records a batch of asset position updates.
	/// </summary>
	/// <param name="updates">A collection of AssetPosition objects.</param>
	Task RecordAssetPositionsAsync(IEnumerable<AssetPosition> updates);

	#endregion
}
