﻿using System;
using System.Linq;
using DynamicData.Kernel;
using Xunit;
using FluentAssertions;

namespace DynamicData.Tests.Cache
{
    public class LeftJoinFixture: IDisposable
    {
        private readonly SourceCache<Device, string> _left;
        private readonly SourceCache<DeviceMetaData, string> _right;
        private readonly ChangeSetAggregator<DeviceWithMetadata, string> _result;

        public  LeftJoinFixture()
        {
            _left = new SourceCache<Device, string>(device => device.Name);
            _right = new SourceCache<DeviceMetaData, string>(device => device.Name);

            _result = _left.Connect()
                            .LeftJoin(_right.Connect(), meta => meta.Name, (key, device, meta) => new DeviceWithMetadata(device, meta))
                            .AsAggregator();
        }

        public void Dispose()
        {
            _left.Dispose();
            _right.Dispose();
            _result.Dispose();
        }

        [Fact]
        public void AddLeftOnly()
        {
            _left.Edit(innerCache =>
            {
                innerCache.AddOrUpdate(new Device("Device1"));
                innerCache.AddOrUpdate(new Device("Device2"));
                innerCache.AddOrUpdate(new Device("Device3"));
            });

            3.Should().Be(_result.Data.Count);
            _result.Data.Lookup("Device1").HasValue.Should().BeTrue();
            _result.Data.Lookup("Device2").HasValue.Should().BeTrue();
            _result.Data.Lookup("Device3").HasValue.Should().BeTrue();

            _result.Data.Items.All(dwm => dwm.MetaData == Optional<DeviceMetaData>.None).Should().BeTrue();
        }

        [Fact]
        public void AddRightOnly()
        {
            _right.Edit(innerCache =>
            {
                innerCache.AddOrUpdate(new DeviceMetaData("Device1"));
                innerCache.AddOrUpdate(new DeviceMetaData("Device2"));
                innerCache.AddOrUpdate(new DeviceMetaData("Device3"));
            });

            0.Should().Be(_result.Data.Count);
        }

        [Fact]
        public void AddLetThenRight()
        {
            _left.Edit(innerCache =>
            {
                innerCache.AddOrUpdate(new Device("Device1"));
                innerCache.AddOrUpdate(new Device("Device2"));
                innerCache.AddOrUpdate(new Device("Device3"));
            });

            _right.Edit(innerCache =>
            {
                innerCache.AddOrUpdate(new DeviceMetaData("Device1"));
                innerCache.AddOrUpdate(new DeviceMetaData("Device2"));
                innerCache.AddOrUpdate(new DeviceMetaData("Device3"));
            });

            3.Should().Be(_result.Data.Count);

            _result.Data.Items.All(dwm => dwm.MetaData != Optional<DeviceMetaData>.None).Should().BeTrue();
        }

        [Fact]
        public void RemoveVarious()
        {
            _left.Edit(innerCache =>
            {
                innerCache.AddOrUpdate(new Device("Device1"));
                innerCache.AddOrUpdate(new Device("Device2"));
                innerCache.AddOrUpdate(new Device("Device3"));
            });

            _right.Edit(innerCache =>
            {
                innerCache.AddOrUpdate(new DeviceMetaData("Device1"));
                innerCache.AddOrUpdate(new DeviceMetaData("Device2"));
                innerCache.AddOrUpdate(new DeviceMetaData("Device3"));
            });

            _right.Remove("Device3");

            3.Should().Be(_result.Data.Count);
            2.Should().Be(_result.Data.Items.Count(dwm => dwm.MetaData != Optional<DeviceMetaData>.None));

            _left.Remove("Device1");
            _result.Data.Lookup("Device1").HasValue.Should().BeFalse();
        }

        [Fact]
        public void AddRightThenLeft()
        {
            _right.Edit(innerCache =>
            {
                innerCache.AddOrUpdate(new DeviceMetaData("Device1"));
                innerCache.AddOrUpdate(new DeviceMetaData("Device2"));
                innerCache.AddOrUpdate(new DeviceMetaData("Device3"));
            });

            _left.Edit(innerCache =>
            {
                innerCache.AddOrUpdate(new Device("Device1"));
                innerCache.AddOrUpdate(new Device("Device2"));
                innerCache.AddOrUpdate(new Device("Device3"));
            });

            3.Should().Be(_result.Data.Count);

            _result.Data.Items.All(dwm => dwm.MetaData != Optional<DeviceMetaData>.None).Should().BeTrue();
        }

        [Fact]
        public void UpdateRight()
        {
            _right.Edit(innerCache =>
            {
                innerCache.AddOrUpdate(new DeviceMetaData("Device1"));
                innerCache.AddOrUpdate(new DeviceMetaData("Device2"));
                innerCache.AddOrUpdate(new DeviceMetaData("Device3"));
            });

            _left.Edit(innerCache =>
            {
                innerCache.AddOrUpdate(new Device("Device1"));
                innerCache.AddOrUpdate(new Device("Device2"));
                innerCache.AddOrUpdate(new Device("Device3"));
            });

            3.Should().Be(_result.Data.Count);

            _result.Data.Items.All(dwm => dwm.MetaData != Optional<DeviceMetaData>.None).Should().BeTrue();
        }

        public class Device : IEquatable<Device>
        {
            public string Name { get; }

            public Device(string name)
            {
                Name = name;
            }

            #region Equality Members

            public bool Equals(Device other)
            {
                if (ReferenceEquals(null, other))
                {
                    return false;
                }

                if (ReferenceEquals(this, other))
                {
                    return true;
                }

                return string.Equals(Name, other.Name);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                {
                    return false;
                }

                if (ReferenceEquals(this, obj))
                {
                    return true;
                }

                if (obj.GetType() != GetType())
                {
                    return false;
                }

                return Equals((Device)obj);
            }

            public override int GetHashCode()
            {
                return (Name != null ? Name.GetHashCode() : 0);
            }

            public static bool operator ==(Device left, Device right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(Device left, Device right)
            {
                return !Equals(left, right);
            }

            #endregion

            public override string ToString()
            {
                return $"{Name}";
            }
        }

        public class DeviceMetaData : IEquatable<DeviceMetaData>
        {
            public string Name { get; }

            public bool IsAutoConnect { get; }

            public DeviceMetaData(string name, bool isAutoConnect = false)
            {
                Name = name;
                IsAutoConnect = isAutoConnect;
            }

            #region Equality members

            public bool Equals(DeviceMetaData other)
            {
                if (ReferenceEquals(null, other))
                {
                    return false;
                }

                if (ReferenceEquals(this, other))
                {
                    return true;
                }

                return string.Equals(Name, other.Name) && IsAutoConnect == other.IsAutoConnect;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                {
                    return false;
                }

                if (ReferenceEquals(this, obj))
                {
                    return true;
                }

                if (obj.GetType() != GetType())
                {
                    return false;
                }

                return Equals((DeviceMetaData)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((Name != null ? Name.GetHashCode() : 0) * 397) ^ IsAutoConnect.GetHashCode();
                }
            }

            public static bool operator ==(DeviceMetaData left, DeviceMetaData right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(DeviceMetaData left, DeviceMetaData right)
            {
                return !Equals(left, right);
            }

            #endregion

            public override string ToString()
            {
                return $"Metadata: {Name}. IsAutoConnect = {IsAutoConnect}";
            }
        }

        public class DeviceWithMetadata : IEquatable<DeviceWithMetadata>
        {
            public Device Device { get; }
            public Optional<DeviceMetaData> MetaData { get; }

            public DeviceWithMetadata(Device device, Optional<DeviceMetaData> metaData)
            {
                Device = device;
                MetaData = metaData;
            }

            #region Equality members

            public bool Equals(DeviceWithMetadata other)
            {
                if (ReferenceEquals(null, other))
                {
                    return false;
                }

                if (ReferenceEquals(this, other))
                {
                    return true;
                }

                return Equals(Device, other.Device) && MetaData.Equals(other.MetaData);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                {
                    return false;
                }

                if (ReferenceEquals(this, obj))
                {
                    return true;
                }

                if (obj.GetType() != GetType())
                {
                    return false;
                }

                return Equals((DeviceWithMetadata)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((Device != null ? Device.GetHashCode() : 0) * 397) ^ MetaData.GetHashCode();
                }
            }

            public static bool operator ==(DeviceWithMetadata left, DeviceWithMetadata right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(DeviceWithMetadata left, DeviceWithMetadata right)
            {
                return !Equals(left, right);
            }

            #endregion

            public override string ToString()
            {
                return $"{Device} ({MetaData})";
            }
        }
    }
}
