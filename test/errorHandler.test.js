const errorHandler = require('../middleware/errorHandler');

describe('errorHandler middleware', () => {
  let res;
  let req;
  let next;

  beforeEach(() => {
    res = {
      status: jest.fn().mockReturnThis(),
      json: jest.fn()
    };
    req = {};
    next = jest.fn();
    jest.clearAllMocks();
  });

  test('responds with provided err.status, message, code and details', () => {
    const err = { status: 418, message: 'I am a teapot', code: 'TEAPOT', details: { kettle: false } };

    errorHandler(err, req, res, next);

    expect(res.status).toHaveBeenCalledWith(418);
    expect(res.json).toHaveBeenCalledTimes(1);
    expect(res.json).toHaveBeenCalledWith({
      error: {
        message: 'I am a teapot',
        code: 'TEAPOT',
        details: { kettle: false }
      }
    });
  });

  test('falls back to defaults when err has missing fields', () => {
    const err = {};

    errorHandler(err, req, res, next);

    expect(res.status).toHaveBeenCalledWith(500);
    expect(res.json).toHaveBeenCalledTimes(1);
    expect(res.json).toHaveBeenCalledWith({
      error: {
        message: 'Internal Server Error',
        code: 'SERVER_ERROR',
        details: null
      }
    });
  });
});
