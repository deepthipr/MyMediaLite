<%@ WebService Language="C#" Class="MyMediaLite.RatingService" %>

using System;
using System.Collections.Generic;
using System.Web.Services;
using MyMediaLite.Data;
using MyMediaLite.RatingPrediction;
using MyMediaLite.Util;

// TODO
//  - database backend
//  - load model on startup
//  - string user and item IDs

namespace MyMediaLite
{
	[WebService (Namespace = "http://ismll.de/RatingService")]
	public class RatingService
	{
		static RatingPredictor recommender;
		static EntityMapping user_mapping;
		static EntityMapping item_mapping;
	
		static int access_counter;
	
		public RatingService()
		{
			if (recommender == null)
			{
				Console.Error.Write("Setting up recommender ... ");
				
				access_counter = 0;
				
				recommender  = new BiasedMatrixFactorization();
				user_mapping = new EntityMapping();
				item_mapping = new EntityMapping();
				
				recommender.Ratings = new Ratings();
				recommender.Train();
				Console.Error.WriteLine("done.");
			}
		}

		[WebMethod]
		public void AddBulkFeedback(int user_id, List<int> item_ids, List<double> scores)
		{
		}

		[WebMethod]
		public void AddFeedbackNoTraining(int user_id, int item_id, double score)
		{
			if (access_counter % 100 == 99)
				Console.Error.Write(".");
			if (access_counter % 8000 == 7999)						
				Console.Error.WriteLine();
			access_counter++;						
						
			// TODO check whether score is in valid range
			recommender.Ratings.Add(user_mapping.ToInternalID(user_id), item_mapping.ToInternalID(item_id), score);
		}		
						
		[WebMethod]
		public void AddFeedback(int user_id, int item_id, double score)
		{
			// TODO check whether score is in valid range
			recommender.AddRating(user_mapping.ToInternalID(user_id), item_mapping.ToInternalID(item_id), score);
		}
		
		[WebMethod]
		public double Predict(int user_id, int item_id)
		{
			return recommender.Predict(user_mapping.ToInternalID(user_id), item_mapping.ToInternalID(item_id));
		}
		
		[WebMethod]
		public void Train()
		{
			Utils.DisplayDataStats(recommender.Ratings, null, recommender);
			recommender.Train();
		}		
	}
}