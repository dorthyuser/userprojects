const { SalesforceClient } = require('../utils/salesforceClient');
const { ValidationError, NotFoundError } = require('../utils/errors');

class HotelService {
  static async searchHotels(query) {
    const city = String(query.city || '').trim();
    const checkInDate = String(query.checkInDate || '').trim();
    const checkOutDate = String(query.checkOutDate || '').trim();
    const guests = Number(query.guests);
    const page = query.page ? Number(query.page) : 1;
    const pageSize = query.pageSize ? Number(query.pageSize) : Number(process.env.DEFAULT_PAGE_SIZE || 20);

    if (!city || !checkInDate || !checkOutDate || !Number.isInteger(guests) || guests < 1) throw new ValidationError('Invalid search parameters');
    if (!/^[0-9]{4}-[0-9]{2}-[0-9]{2}$/.test(checkInDate) || !/^[0-9]{4}-[0-9]{2}-[0-9]{2}$/.test(checkOutDate)) throw new ValidationError('Invalid date format');
    if (page < 1 || pageSize < 1 || pageSize > Number(process.env.MAX_PAGE_SIZE || 100)) throw new ValidationError('Invalid pagination');

    const client = await SalesforceClient.getClient();
    const hotels = await client.query(`SELECT Id, Name, City__c, Available_Rooms__c, Min_Nightly_Rate__c, Currency__c FROM Hotel__c WHERE City__c = '${city.replace(/'/g, "''")}' AND Is_Active__c = true ORDER BY Name ASC`);
    const records = (hotels && hotels.records) ? hotels.records : [];
    const data = records.map((r) => ({
      hotelId: r.Id,
      hotelName: r.Name,
      city: r.City__c,
      availableRooms: r.Available_Rooms__c,
      minNightlyRate: r.Min_Nightly_Rate__c,
      currency: r.Currency__c
    }));

    if (data.length === 0) throw new NotFoundError('No hotels found');

    const start = (page - 1) * pageSize;
    const paged = data.slice(start, start + pageSize);
    return { data: paged, page, pageSize, totalItems: data.length, totalPages: Math.max(1, Math.ceil(data.length / pageSize)) };
  }
}

module.exports = { HotelService };