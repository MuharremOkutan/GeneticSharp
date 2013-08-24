﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HelperSharp;
using GeneticSharp.Domain.Chromosomes;

namespace GeneticSharp.Domain.Crossovers
{
	/// <summary>
	/// A base class for crossovers.
	/// </summary>
    public abstract class CrossoverBase : ICrossover
    {
        #region Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="GeneticSharp.Domain.Crossovers.CrossoverBase"/> class.
		/// </summary>
		/// <param name="parentsNumber">The number of parents need for cross.</param>
		/// <param name="childrenNumber">The number of children generated by cross.</param>
        protected CrossoverBase(int parentsNumber, int childrenNumber) : this(parentsNumber, childrenNumber, 2)
        {
	        ParentsNumber = parentsNumber;
			ChildrenNumber = childrenNumber;
        }

		/// <summary>
		/// Initializes a new instance of the <see cref="GeneticSharp.Domain.Crossovers.CrossoverBase"/> class.
		/// </summary>
		/// <param name="parentsNumber">The number of parents need for cross.</param>
		/// <param name="childrenNumber">The number of children generated by cross.</param>
		/// <param name="minChromosomeLength">The minimum length of the chromosome supported by the crossover.</param>
		protected CrossoverBase(int parentsNumber, int childrenNumber, int minChromosomeLength)
		{
			ParentsNumber = parentsNumber;
			ChildrenNumber = childrenNumber;
			MinChromosomeLength = minChromosomeLength;
		}
        #endregion

		#region Properties
        /// <summary>
        /// Gets if the operator is ordered (if can keep the chromosome order).
        /// </summary>
        public bool IsOrdered { get; protected set; }
        
		/// <summary>
		/// Gets the number of parents need for cross.
		/// </summary>
		/// <value>The parents number.</value>
        public int ParentsNumber { get; private set; }

		/// <summary>
		/// Gets the number of children generated by cross.
		/// </summary>
		/// <value>The children number.</value>
		public int ChildrenNumber  { get; private set; }

		/// <summary>
		/// Gets the minimum length of the chromosome supported by the crossover.
		/// </summary>
		/// <value>The minimum length of the chromosome.</value>
		public int MinChromosomeLength  { get; private set; }
		#endregion

		#region Methods
		/// <summary>
		/// Cross the specified parents generating the children.
		/// </summary>
		/// <param name="parents">Parents.</param>
		/// <returns>The offspring (children) of the parents.</returns>
        public IList<IChromosome> Cross(IList<IChromosome> parents)
        {
            ExceptionHelper.ThrowIfNull("parents", parents);

            if (parents.Count != ParentsNumber)
            {
                throw new ArgumentOutOfRangeException("parents", "The number of parents should be the same of ParentsNumber.");
            }

			var firstParent = parents [0];

			if (firstParent.Length < MinChromosomeLength) {
				throw new CrossoverException(this, "A chromosome should have, at least, {0} genes. {1} has only {2} gene."
				                             .With(MinChromosomeLength, firstParent.GetType().Name, firstParent.Length));
			}

            return PerformCross(parents);
        }

		/// <summary>
		/// Performs the cross with specified parents generating the children.
		/// </summary>
		/// <param name="parents">Parents.</param>
		/// <returns>The offspring (children) of the parents.</returns>
        protected abstract IList<IChromosome> PerformCross(IList<IChromosome> parents);
        #endregion        
    }
}
