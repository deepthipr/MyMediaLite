// Copyright (C) 2010 Zeno Gantner
//
// This file is part of MyMediaLite.
//
// MyMediaLite is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// MyMediaLite is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with MyMediaLite.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Globalization;
using MyMediaLite.Data;
using MyMediaLite.DataType;
using SVM;


namespace MyMediaLite.ItemRecommender
{
	/// <summary>
	/// Content-based filtering using one support-vector machine per user
	/// </summary>
    /// <remarks>
    /// This engine does not support online updates.
    /// </remarks>
    public class ItemAttributeSVM : Memory, IItemAttributeAwareRecommender
    {
		/// <inheritdoc/>
		public SparseBooleanMatrix ItemAttributes
		{
			get { return this.item_attributes; }
			set
			{
				this.item_attributes = value;
				this.NumItemAttributes = item_attributes.NumberOfColumns;
				this.MaxItemID = Math.Max(MaxItemID, item_attributes.NumberOfRows - 1);
			}
		}
		private SparseBooleanMatrix item_attributes;		
		
		/// <inheritdoc/>
	    public int NumItemAttributes { get;	set; }

		/// <summary>C hyperparameter for the SVM</summary>
		public double C { get { return c; } set { c = value; } }
		double c = 1;

		/// <summary>Gamma parameter for RBF kernel</summary>
		public double Gamma { get {	return gamma; }	set { gamma = value; } }
		double gamma = (double) 1 / 500;

		private SVM.Model[] models;

        /// <inheritdoc/>
        public override void Train()
        {
			int num_users = data_user.NumberOfRows;
			int num_items = data_item.NumberOfRows;

			var svm_features = new List<Node[]>();

			Node[][] svm_features_array = svm_features.ToArray();
			Parameter svm_parameters = new Parameter();
			svm_parameters.SvmType = SvmType.EPSILON_SVR;
			//svm_parameters.SvmType = SvmType.NU_SVR;
			svm_parameters.C     = this.c;
			svm_parameters.Gamma = this.gamma;

			// user-wise training
			models = new Model[num_users];
			for (int u = 0; u < num_users; u++)
			{
				double[] targets = new double[num_items];
				for (int i = 0; i < num_items; i++)
					targets[i] = data_user[u, i] ? 1 : 0;

				Problem svm_problem = new Problem(svm_features.Count, targets, svm_features_array, NumItemAttributes - 1); // TODO check
				models[u] = SVM.Training.Train(svm_problem, svm_parameters);
			}
        }

		// TODO share this among different classes
		private Node[] CreateNodes(int item_id)
		{
			// create attribute representation digestible by LIBSVM
			HashSet<int> attributes = this.ItemAttributes[item_id];
			Node[] item_svm_data = new Node[attributes.Count];
			int counter = 0;
			foreach (int attr in attributes)
				item_svm_data[counter++] = new Node(attr, 1.0);
			return item_svm_data;
		}

		/// <inheritdoc/>
		public override double Predict(int user_id, int item_id)
		{
			// TODO speed improvement: do not create nodes on the fly
			return SVM.Prediction.Predict(models[user_id], CreateNodes(item_id));
			// TODO make sure we return score, not class
		}

		/// <inheritdoc/>
		public override void SaveModel(string filename)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc/>
		public override void LoadModel(string filename)
		{
			throw new NotImplementedException();
		}

        /// <inheritdoc/>
		public override string ToString()
		{
			var ni = new NumberFormatInfo();
			ni.NumberDecimalDigits = '.';

			return string.Format(ni, "item-attribute-SVM C={0} Gamma={1}", c, gamma);
		}
	}
}