function (accessToken, context, callback) {
  request.get(
    {
      url: 'https://secure.splitwise.com/api/v3.0/get_current_user',
      headers: {
        'Authorization': 'Bearer ' + accessToken,
      }
    },
    (err, resp, body) => {
      if (err) {
        return callback(err);
      }

      if (resp.statusCode !== 200) {
        return callback(new Error(body));
      }

      let parsedBody;

      try {
        parsedBody = JSON.parse(body);
      } catch (jsonError) {
        return callback(new Error(body));
      }

      const firstName = parsedBody.user.first_name
      const lastName = parsedBody.user.last_name
      const fullName = `${firstName} ${lastName}`

      const profile = {
        user_id: parsedBody.user.id,
        email: parsedBody.user.email,
        given_name: firstName,
        family_name: lastName,
        name: fullName
      };

      callback(null, profile);
    }
  );
}