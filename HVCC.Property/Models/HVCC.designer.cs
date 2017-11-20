﻿#pragma warning disable 1591
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace HVCC.Property.Models
{
	using System.Data.Linq;
	using System.Data.Linq.Mapping;
	using System.Data;
	using System.Collections.Generic;
	using System.Reflection;
	using System.Linq;
	using System.Linq.Expressions;
	using System.ComponentModel;
	using System;
	
	
	[global::System.Data.Linq.Mapping.DatabaseAttribute(Name="HVCC")]
	public partial class HVCCDataContext : System.Data.Linq.DataContext
	{
		
		private static System.Data.Linq.Mapping.MappingSource mappingSource = new AttributeMappingSource();
		
    #region Extensibility Method Definitions
    partial void OnCreated();
    partial void InsertRelationship(Relationship instance);
    partial void UpdateRelationship(Relationship instance);
    partial void DeleteRelationship(Relationship instance);
    partial void InsertAnnualPropertyInformation(AnnualPropertyInformation instance);
    partial void UpdateAnnualPropertyInformation(AnnualPropertyInformation instance);
    partial void DeleteAnnualPropertyInformation(AnnualPropertyInformation instance);
    partial void InsertProperty(Property instance);
    partial void UpdateProperty(Property instance);
    partial void DeleteProperty(Property instance);
    #endregion
		
		public HVCCDataContext() : 
				base(global::HVCC.Property.Properties.Settings.Default.HVCCConnectionString, mappingSource)
		{
			OnCreated();
		}
		
		public HVCCDataContext(string connection) : 
				base(connection, mappingSource)
		{
			OnCreated();
		}
		
		public HVCCDataContext(System.Data.IDbConnection connection) : 
				base(connection, mappingSource)
		{
			OnCreated();
		}
		
		public HVCCDataContext(string connection, System.Data.Linq.Mapping.MappingSource mappingSource) : 
				base(connection, mappingSource)
		{
			OnCreated();
		}
		
		public HVCCDataContext(System.Data.IDbConnection connection, System.Data.Linq.Mapping.MappingSource mappingSource) : 
				base(connection, mappingSource)
		{
			OnCreated();
		}
		
		public System.Data.Linq.Table<Relationship> Relationships
		{
			get
			{
				return this.GetTable<Relationship>();
			}
		}
		
		public System.Data.Linq.Table<AnnualPropertyInformation> AnnualPropertyInformations
		{
			get
			{
				return this.GetTable<AnnualPropertyInformation>();
			}
		}
		
		public System.Data.Linq.Table<Property> Properties
		{
			get
			{
				return this.GetTable<Property>();
			}
		}
		
		public System.Data.Linq.Table<v_PropertyHistory> v_PropertyHistories
		{
			get
			{
				return this.GetTable<v_PropertyHistory>();
			}
		}
		
		[global::System.Data.Linq.Mapping.FunctionAttribute(Name="dbo.usp_GetRelationsForProperty")]
		public ISingleResult<usp_GetRelationsForPropertyResult> usp_GetRelationsForProperty([global::System.Data.Linq.Mapping.ParameterAttribute(Name="Section", DbType="Int")] System.Nullable<int> section, [global::System.Data.Linq.Mapping.ParameterAttribute(Name="Block", DbType="Int")] System.Nullable<int> block, [global::System.Data.Linq.Mapping.ParameterAttribute(Name="Lot", DbType="Int")] System.Nullable<int> lot)
		{
			IExecuteResult result = this.ExecuteMethodCall(this, ((MethodInfo)(MethodInfo.GetCurrentMethod())), section, block, lot);
			return ((ISingleResult<usp_GetRelationsForPropertyResult>)(result.ReturnValue));
		}
	}
	
	[global::System.Data.Linq.Mapping.TableAttribute(Name="dbo.Relationships")]
	public partial class Relationship : INotifyPropertyChanging, INotifyPropertyChanged
	{
		
		private static PropertyChangingEventArgs emptyChangingEventArgs = new PropertyChangingEventArgs(String.Empty);
		
		private int _RelationshipID;
		
		private int _PropertyID;
		
		private string _FName;
		
		private string _LName;
		
		private string _RelationToOwner;
		
		private string _PhotoURI;
		
		private EntityRef<Property> _Property;
		
    #region Extensibility Method Definitions
    partial void OnLoaded();
    partial void OnValidate(System.Data.Linq.ChangeAction action);
    partial void OnCreated();
    partial void OnRelationshipIDChanging(int value);
    partial void OnRelationshipIDChanged();
    partial void OnPropertyIDChanging(int value);
    partial void OnPropertyIDChanged();
    partial void OnFNameChanging(string value);
    partial void OnFNameChanged();
    partial void OnLNameChanging(string value);
    partial void OnLNameChanged();
    partial void OnRelationToOwnerChanging(string value);
    partial void OnRelationToOwnerChanged();
    partial void OnPhotoURIChanging(string value);
    partial void OnPhotoURIChanged();
    #endregion
		
		public Relationship()
		{
			this._Property = default(EntityRef<Property>);
			OnCreated();
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_RelationshipID", AutoSync=AutoSync.OnInsert, DbType="Int NOT NULL IDENTITY", IsPrimaryKey=true, IsDbGenerated=true)]
		public int RelationshipID
		{
			get
			{
				return this._RelationshipID;
			}
			set
			{
				if ((this._RelationshipID != value))
				{
					this.OnRelationshipIDChanging(value);
					this.SendPropertyChanging();
					this._RelationshipID = value;
					this.SendPropertyChanged("RelationshipID");
					this.OnRelationshipIDChanged();
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_PropertyID", DbType="Int NOT NULL")]
		public int PropertyID
		{
			get
			{
				return this._PropertyID;
			}
			set
			{
				if ((this._PropertyID != value))
				{
					if (this._Property.HasLoadedOrAssignedValue)
					{
						throw new System.Data.Linq.ForeignKeyReferenceAlreadyHasValueException();
					}
					this.OnPropertyIDChanging(value);
					this.SendPropertyChanging();
					this._PropertyID = value;
					this.SendPropertyChanged("PropertyID");
					this.OnPropertyIDChanged();
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_FName", DbType="VarChar(50) NOT NULL", CanBeNull=false)]
		public string FName
		{
			get
			{
				return this._FName;
			}
			set
			{
				if ((this._FName != value))
				{
					this.OnFNameChanging(value);
					this.SendPropertyChanging();
					this._FName = value;
					this.SendPropertyChanged("FName");
					this.OnFNameChanged();
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_LName", DbType="VarChar(50) NOT NULL", CanBeNull=false)]
		public string LName
		{
			get
			{
				return this._LName;
			}
			set
			{
				if ((this._LName != value))
				{
					this.OnLNameChanging(value);
					this.SendPropertyChanging();
					this._LName = value;
					this.SendPropertyChanged("LName");
					this.OnLNameChanged();
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_RelationToOwner", DbType="VarChar(50) NOT NULL", CanBeNull=false)]
		public string RelationToOwner
		{
			get
			{
				return this._RelationToOwner;
			}
			set
			{
				if ((this._RelationToOwner != value))
				{
					this.OnRelationToOwnerChanging(value);
					this.SendPropertyChanging();
					this._RelationToOwner = value;
					this.SendPropertyChanged("RelationToOwner");
					this.OnRelationToOwnerChanged();
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_PhotoURI", DbType="VarChar(250)")]
		public string PhotoURI
		{
			get
			{
				return this._PhotoURI;
			}
			set
			{
				if ((this._PhotoURI != value))
				{
					this.OnPhotoURIChanging(value);
					this.SendPropertyChanging();
					this._PhotoURI = value;
					this.SendPropertyChanged("PhotoURI");
					this.OnPhotoURIChanged();
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.AssociationAttribute(Name="Property_Relationship", Storage="_Property", ThisKey="PropertyID", OtherKey="PropertyID", IsForeignKey=true)]
		public Property Property
		{
			get
			{
				return this._Property.Entity;
			}
			set
			{
				Property previousValue = this._Property.Entity;
				if (((previousValue != value) 
							|| (this._Property.HasLoadedOrAssignedValue == false)))
				{
					this.SendPropertyChanging();
					if ((previousValue != null))
					{
						this._Property.Entity = null;
						previousValue.Relationships.Remove(this);
					}
					this._Property.Entity = value;
					if ((value != null))
					{
						value.Relationships.Add(this);
						this._PropertyID = value.PropertyID;
					}
					else
					{
						this._PropertyID = default(int);
					}
					this.SendPropertyChanged("Property");
				}
			}
		}
		
		public event PropertyChangingEventHandler PropertyChanging;
		
		public event PropertyChangedEventHandler PropertyChanged;
		
		protected virtual void SendPropertyChanging()
		{
			if ((this.PropertyChanging != null))
			{
				this.PropertyChanging(this, emptyChangingEventArgs);
			}
		}
		
		protected virtual void SendPropertyChanged(String propertyName)
		{
			if ((this.PropertyChanged != null))
			{
				this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}
	}
	
	[global::System.Data.Linq.Mapping.TableAttribute(Name="dbo.AnnualPropertyInformation")]
	public partial class AnnualPropertyInformation : INotifyPropertyChanging, INotifyPropertyChanged
	{
		
		private static PropertyChangingEventArgs emptyChangingEventArgs = new PropertyChangingEventArgs(String.Empty);
		
		private int _RowID;
		
		private int _PropertyID;
		
		private int _Year;
		
		private decimal _AmountOwed;
		
		private bool _IsInGoodStanding;
		
		private int _NumGolfCart;
		
		private decimal _CartFeesOwed;
		
		private EntityRef<Property> _Property;
		
    #region Extensibility Method Definitions
    partial void OnLoaded();
    partial void OnValidate(System.Data.Linq.ChangeAction action);
    partial void OnCreated();
    partial void OnRowIDChanging(int value);
    partial void OnRowIDChanged();
    partial void OnPropertyIDChanging(int value);
    partial void OnPropertyIDChanged();
    partial void OnYearChanging(int value);
    partial void OnYearChanged();
    partial void OnAmountOwedChanging(decimal value);
    partial void OnAmountOwedChanged();
    partial void OnIsInGoodStandingChanging(bool value);
    partial void OnIsInGoodStandingChanged();
    partial void OnNumGolfCartChanging(int value);
    partial void OnNumGolfCartChanged();
    partial void OnCartFeesOwedChanging(decimal value);
    partial void OnCartFeesOwedChanged();
    #endregion
		
		public AnnualPropertyInformation()
		{
			this._Property = default(EntityRef<Property>);
			OnCreated();
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_RowID", AutoSync=AutoSync.OnInsert, DbType="Int NOT NULL IDENTITY", IsPrimaryKey=true, IsDbGenerated=true)]
		public int RowID
		{
			get
			{
				return this._RowID;
			}
			set
			{
				if ((this._RowID != value))
				{
					this.OnRowIDChanging(value);
					this.SendPropertyChanging();
					this._RowID = value;
					this.SendPropertyChanged("RowID");
					this.OnRowIDChanged();
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_PropertyID", DbType="Int NOT NULL")]
		public int PropertyID
		{
			get
			{
				return this._PropertyID;
			}
			set
			{
				if ((this._PropertyID != value))
				{
					if (this._Property.HasLoadedOrAssignedValue)
					{
						throw new System.Data.Linq.ForeignKeyReferenceAlreadyHasValueException();
					}
					this.OnPropertyIDChanging(value);
					this.SendPropertyChanging();
					this._PropertyID = value;
					this.SendPropertyChanged("PropertyID");
					this.OnPropertyIDChanged();
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_Year", DbType="Int NOT NULL")]
		public int Year
		{
			get
			{
				return this._Year;
			}
			set
			{
				if ((this._Year != value))
				{
					this.OnYearChanging(value);
					this.SendPropertyChanging();
					this._Year = value;
					this.SendPropertyChanged("Year");
					this.OnYearChanged();
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_AmountOwed", DbType="Money NOT NULL")]
		public decimal AmountOwed
		{
			get
			{
				return this._AmountOwed;
			}
			set
			{
				if ((this._AmountOwed != value))
				{
					this.OnAmountOwedChanging(value);
					this.SendPropertyChanging();
					this._AmountOwed = value;
					this.SendPropertyChanged("AmountOwed");
					this.OnAmountOwedChanged();
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_IsInGoodStanding", DbType="Bit NOT NULL")]
		public bool IsInGoodStanding
		{
			get
			{
				return this._IsInGoodStanding;
			}
			set
			{
				if ((this._IsInGoodStanding != value))
				{
					this.OnIsInGoodStandingChanging(value);
					this.SendPropertyChanging();
					this._IsInGoodStanding = value;
					this.SendPropertyChanged("IsInGoodStanding");
					this.OnIsInGoodStandingChanged();
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_NumGolfCart", DbType="Int NOT NULL")]
		public int NumGolfCart
		{
			get
			{
				return this._NumGolfCart;
			}
			set
			{
				if ((this._NumGolfCart != value))
				{
					this.OnNumGolfCartChanging(value);
					this.SendPropertyChanging();
					this._NumGolfCart = value;
					this.SendPropertyChanged("NumGolfCart");
					this.OnNumGolfCartChanged();
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_CartFeesOwed", DbType="Money NOT NULL")]
		public decimal CartFeesOwed
		{
			get
			{
				return this._CartFeesOwed;
			}
			set
			{
				if ((this._CartFeesOwed != value))
				{
					this.OnCartFeesOwedChanging(value);
					this.SendPropertyChanging();
					this._CartFeesOwed = value;
					this.SendPropertyChanged("CartFeesOwed");
					this.OnCartFeesOwedChanged();
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.AssociationAttribute(Name="Property_AnnualPropertyInformation", Storage="_Property", ThisKey="PropertyID", OtherKey="PropertyID", IsForeignKey=true)]
		public Property Property
		{
			get
			{
				return this._Property.Entity;
			}
			set
			{
				Property previousValue = this._Property.Entity;
				if (((previousValue != value) 
							|| (this._Property.HasLoadedOrAssignedValue == false)))
				{
					this.SendPropertyChanging();
					if ((previousValue != null))
					{
						this._Property.Entity = null;
						previousValue.AnnualPropertyInformations.Remove(this);
					}
					this._Property.Entity = value;
					if ((value != null))
					{
						value.AnnualPropertyInformations.Add(this);
						this._PropertyID = value.PropertyID;
					}
					else
					{
						this._PropertyID = default(int);
					}
					this.SendPropertyChanged("Property");
				}
			}
		}
		
		public event PropertyChangingEventHandler PropertyChanging;
		
		public event PropertyChangedEventHandler PropertyChanged;
		
		protected virtual void SendPropertyChanging()
		{
			if ((this.PropertyChanging != null))
			{
				this.PropertyChanging(this, emptyChangingEventArgs);
			}
		}
		
		protected virtual void SendPropertyChanged(String propertyName)
		{
			if ((this.PropertyChanged != null))
			{
				this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}
	}
	
	[global::System.Data.Linq.Mapping.TableAttribute(Name="dbo.Properties")]
	public partial class Property : INotifyPropertyChanging, INotifyPropertyChanged
	{
		
		private static PropertyChangingEventArgs emptyChangingEventArgs = new PropertyChangingEventArgs(String.Empty);
		
		private int _PropertyID;
		
		private int _Section;
		
		private int _Block;
		
		private int _Lot;
		
		private int _SubLot;
		
		private string _Notes;
		
		private EntitySet<Relationship> _Relationships;
		
		private EntitySet<AnnualPropertyInformation> _AnnualPropertyInformations;
		
    #region Extensibility Method Definitions
    partial void OnLoaded();
    partial void OnValidate(System.Data.Linq.ChangeAction action);
    partial void OnCreated();
    partial void OnPropertyIDChanging(int value);
    partial void OnPropertyIDChanged();
    partial void OnSectionChanging(int value);
    partial void OnSectionChanged();
    partial void OnBlockChanging(int value);
    partial void OnBlockChanged();
    partial void OnLotChanging(int value);
    partial void OnLotChanged();
    partial void OnSubLotChanging(int value);
    partial void OnSubLotChanged();
    partial void OnNotesChanging(string value);
    partial void OnNotesChanged();
    #endregion
		
		public Property()
		{
			this._Relationships = new EntitySet<Relationship>(new Action<Relationship>(this.attach_Relationships), new Action<Relationship>(this.detach_Relationships));
			this._AnnualPropertyInformations = new EntitySet<AnnualPropertyInformation>(new Action<AnnualPropertyInformation>(this.attach_AnnualPropertyInformations), new Action<AnnualPropertyInformation>(this.detach_AnnualPropertyInformations));
			OnCreated();
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_PropertyID", AutoSync=AutoSync.OnInsert, DbType="Int NOT NULL IDENTITY", IsPrimaryKey=true, IsDbGenerated=true)]
		public int PropertyID
		{
			get
			{
				return this._PropertyID;
			}
			set
			{
				if ((this._PropertyID != value))
				{
					this.OnPropertyIDChanging(value);
					this.SendPropertyChanging();
					this._PropertyID = value;
					this.SendPropertyChanged("PropertyID");
					this.OnPropertyIDChanged();
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_Section", DbType="Int NOT NULL")]
		public int Section
		{
			get
			{
				return this._Section;
			}
			set
			{
				if ((this._Section != value))
				{
					this.OnSectionChanging(value);
					this.SendPropertyChanging();
					this._Section = value;
					this.SendPropertyChanged("Section");
					this.OnSectionChanged();
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_Block", DbType="Int NOT NULL")]
		public int Block
		{
			get
			{
				return this._Block;
			}
			set
			{
				if ((this._Block != value))
				{
					this.OnBlockChanging(value);
					this.SendPropertyChanging();
					this._Block = value;
					this.SendPropertyChanged("Block");
					this.OnBlockChanged();
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_Lot", DbType="Int NOT NULL")]
		public int Lot
		{
			get
			{
				return this._Lot;
			}
			set
			{
				if ((this._Lot != value))
				{
					this.OnLotChanging(value);
					this.SendPropertyChanging();
					this._Lot = value;
					this.SendPropertyChanged("Lot");
					this.OnLotChanged();
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_SubLot", DbType="Int NOT NULL")]
		public int SubLot
		{
			get
			{
				return this._SubLot;
			}
			set
			{
				if ((this._SubLot != value))
				{
					this.OnSubLotChanging(value);
					this.SendPropertyChanging();
					this._SubLot = value;
					this.SendPropertyChanged("SubLot");
					this.OnSubLotChanged();
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_Notes", DbType="VarChar(500)")]
		public string Notes
		{
			get
			{
				return this._Notes;
			}
			set
			{
				if ((this._Notes != value))
				{
					this.OnNotesChanging(value);
					this.SendPropertyChanging();
					this._Notes = value;
					this.SendPropertyChanged("Notes");
					this.OnNotesChanged();
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.AssociationAttribute(Name="Property_Relationship", Storage="_Relationships", ThisKey="PropertyID", OtherKey="PropertyID")]
		public EntitySet<Relationship> Relationships
		{
			get
			{
				return this._Relationships;
			}
			set
			{
				this._Relationships.Assign(value);
			}
		}
		
		[global::System.Data.Linq.Mapping.AssociationAttribute(Name="Property_AnnualPropertyInformation", Storage="_AnnualPropertyInformations", ThisKey="PropertyID", OtherKey="PropertyID")]
		public EntitySet<AnnualPropertyInformation> AnnualPropertyInformations
		{
			get
			{
				return this._AnnualPropertyInformations;
			}
			set
			{
				this._AnnualPropertyInformations.Assign(value);
			}
		}
		
		public event PropertyChangingEventHandler PropertyChanging;
		
		public event PropertyChangedEventHandler PropertyChanged;
		
		protected virtual void SendPropertyChanging()
		{
			if ((this.PropertyChanging != null))
			{
				this.PropertyChanging(this, emptyChangingEventArgs);
			}
		}
		
		protected virtual void SendPropertyChanged(String propertyName)
		{
			if ((this.PropertyChanged != null))
			{
				this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}
		
		private void attach_Relationships(Relationship entity)
		{
			this.SendPropertyChanging();
			entity.Property = this;
		}
		
		private void detach_Relationships(Relationship entity)
		{
			this.SendPropertyChanging();
			entity.Property = null;
		}
		
		private void attach_AnnualPropertyInformations(AnnualPropertyInformation entity)
		{
			this.SendPropertyChanging();
			entity.Property = this;
		}
		
		private void detach_AnnualPropertyInformations(AnnualPropertyInformation entity)
		{
			this.SendPropertyChanging();
			entity.Property = null;
		}
	}
	
	[global::System.Data.Linq.Mapping.TableAttribute(Name="dbo.v_PropertyHistory")]
	public partial class v_PropertyHistory
	{
		
		private int _Section;
		
		private int _Block;
		
		private int _Lot;
		
		private int _SubLot;
		
		private string _Notes;
		
		private int _Year;
		
		private decimal _AmountOwed;
		
		private bool _IsInGoodStanding;
		
		private int _NumGolfCart;
		
		private decimal _CartFeesOwed;
		
		public v_PropertyHistory()
		{
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_Section", DbType="Int NOT NULL")]
		public int Section
		{
			get
			{
				return this._Section;
			}
			set
			{
				if ((this._Section != value))
				{
					this._Section = value;
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_Block", DbType="Int NOT NULL")]
		public int Block
		{
			get
			{
				return this._Block;
			}
			set
			{
				if ((this._Block != value))
				{
					this._Block = value;
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_Lot", DbType="Int NOT NULL")]
		public int Lot
		{
			get
			{
				return this._Lot;
			}
			set
			{
				if ((this._Lot != value))
				{
					this._Lot = value;
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_SubLot", DbType="Int NOT NULL")]
		public int SubLot
		{
			get
			{
				return this._SubLot;
			}
			set
			{
				if ((this._SubLot != value))
				{
					this._SubLot = value;
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_Notes", DbType="VarChar(500)")]
		public string Notes
		{
			get
			{
				return this._Notes;
			}
			set
			{
				if ((this._Notes != value))
				{
					this._Notes = value;
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_Year", DbType="Int NOT NULL")]
		public int Year
		{
			get
			{
				return this._Year;
			}
			set
			{
				if ((this._Year != value))
				{
					this._Year = value;
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_AmountOwed", DbType="Money NOT NULL")]
		public decimal AmountOwed
		{
			get
			{
				return this._AmountOwed;
			}
			set
			{
				if ((this._AmountOwed != value))
				{
					this._AmountOwed = value;
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_IsInGoodStanding", DbType="Bit NOT NULL")]
		public bool IsInGoodStanding
		{
			get
			{
				return this._IsInGoodStanding;
			}
			set
			{
				if ((this._IsInGoodStanding != value))
				{
					this._IsInGoodStanding = value;
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_NumGolfCart", DbType="Int NOT NULL")]
		public int NumGolfCart
		{
			get
			{
				return this._NumGolfCart;
			}
			set
			{
				if ((this._NumGolfCart != value))
				{
					this._NumGolfCart = value;
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_CartFeesOwed", DbType="Money NOT NULL")]
		public decimal CartFeesOwed
		{
			get
			{
				return this._CartFeesOwed;
			}
			set
			{
				if ((this._CartFeesOwed != value))
				{
					this._CartFeesOwed = value;
				}
			}
		}
	}
	
	public partial class usp_GetRelationsForPropertyResult
	{
		
		private string _FName;
		
		private string _LName;
		
		private string _Relationship;
		
		public usp_GetRelationsForPropertyResult()
		{
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_FName", DbType="VarChar(50) NOT NULL", CanBeNull=false)]
		public string FName
		{
			get
			{
				return this._FName;
			}
			set
			{
				if ((this._FName != value))
				{
					this._FName = value;
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_LName", DbType="VarChar(50) NOT NULL", CanBeNull=false)]
		public string LName
		{
			get
			{
				return this._LName;
			}
			set
			{
				if ((this._LName != value))
				{
					this._LName = value;
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_Relationship", DbType="VarChar(50) NOT NULL", CanBeNull=false)]
		public string Relationship
		{
			get
			{
				return this._Relationship;
			}
			set
			{
				if ((this._Relationship != value))
				{
					this._Relationship = value;
				}
			}
		}
	}
}
#pragma warning restore 1591
