export interface Pagination {
    currentPage: number;
    itemsPerPage: number;
    totalItems: number;
    totalPages: number;
}

export class PaginatedResult<T> {
    result: T;
    pagination: Pagination;
  static result: import("d:/coding/projects/dotnet projects/DatingApp/client/src/app/_models/member").Member[];
  static pagination: Pagination;
}