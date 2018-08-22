export class AppToken {
  accessToken: string;
}

export class UserToken {
  validated: boolean;
  accessToken: string;
  RefreshToken: string;
  ExpireIn: string;
}
